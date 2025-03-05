using Shared;
using TorchSharp;
using TorchSharp.Modules;
using Tensor = TorchSharp.torch.Tensor;
using Settings = NanoGptSetting.Settings;
using LogUtility;
// ReSharper disable All
#pragma warning disable IDE0059

namespace Gpt;

// Translated from python code written by Andrej Karpathy https://www.youtube.com/watch?v=kCc8FmEb1nY
// Comments are a mix of my comments, comments from the video, and GPT-4
// Timestamps of video in comments

/// <summary>
/// Represents a single attention head in the multi-head attention mechanism.
/// It's responsible for calculating self-attention for a given segment of the input features.
/// </summary>
///  Notes about attention - Timestamp: 1:11:15
public sealed class Head : torch.nn.Module
{
    // Linear transformations to project the input tensor into key, query, and value representations.
    private readonly Linear _key;
    private readonly Linear _query;
    private readonly Linear _value;

    // Dropout for regularization to prevent overfitting during training.
    private readonly Dropout _dropout;

    // A mask to ensure the attention mechanism respects the order of words (i.e., a word cannot attend to future words).
    private readonly Tensor _tril;

    /// <param name="headSize">Size/dimension of this head's output.</param>
    public Head(string name) : base(name)
    {
        // _key represents the words or tokens in the input sequence.
        // _query represent what you're trying to find out.
        // _value These are what you get after asking (querying) with the Q and matching with the K.
        // Think of them as the content of the items you're looking up by their labels

        // As the LLM runs, the weights for each of these layers change to represent the context of the words in the sequence.

        // Linear transformation to produce the "key" tensor from the input.
        _key = torch.nn.Linear(Settings.NEmbed, Settings.HeadSize, hasBias: false);
        register_module("key", _key);

        // Linear transformation to produce the "query" tensor from the input.
        _query = torch.nn.Linear(Settings.NEmbed, Settings.HeadSize, hasBias: false);
        register_module("query", _query);

        // Define linear transformation for values, without bias.
        _value = torch.nn.Linear(Settings.NEmbed, Settings.HeadSize, hasBias: false);
        register_module("value", _value);

        // Lower triangular mask to ensure causality in self-attention
        _tril = torch.tril(torch.ones(Settings.BlockSize, Settings.BlockSize));
        register_buffer("tril", _tril);

        // Dropout layer for regularization.
        _dropout = torch.nn.Dropout(Settings.DropoutValue);
        register_module("dropout", _dropout);
    }

    public Tensor Forward(Tensor x)
    {
        // B is batch size, T is sequence length, and C is feature/channel count
        (long B, long T, long C) = (x.size(0), x.size(1), x.size(2));

        // Obtain the key and query representations of the input tensor based on the current weights
        // of the _key and _query layers.
        Tensor k = _key.forward(x); // (B,T,headSize)
        Tensor q = _query.forward(x); // (B,T,headSize)

        // Calculate attention scores based on the dot product of queries and keys. 
        // The scaling factor (k.size(-1))^(-0.5) ensures stability in large dimensions.
        // (B, T, headSize) @ (B, headSize, T) -> (B, T, T)
        // Timestamp: 56:30
        Tensor wei = q.matmul(k.transpose(-2, -1)) * Math.Pow(k.size(-1), -0.5);

        // Using the triangular mask to zero out positions so each character only attends to previous characters (and itself).
        wei = wei.masked_fill(_tril.narrow(0, 0, T).narrow(1, 0, T).eq(0), float.NegativeInfinity);

        // Convert the attention scores ("affinities") to probabilities using the softmax function.
        // This ensures that the attention weights sum to 1 for each sequence.
        wei = torch.nn.functional.softmax(wei, dim: -1);

        // Apply dropout to the attention probabilities. This introduces randomness and 
        // prevents the model from becoming overly reliant on specific attention patterns 
        // in the training data, promoting generalization.
        wei = _dropout.forward(wei);

        // Compute weighted sum of values based on attention scores.
        Tensor v = _value.forward(x); // (B,T, headSize)

        // Use the attention scores to weigh the value representations and produce the output.
        Tensor output = wei.matmul(v); // (B, T, T) @ (B, T, headSize) -> (B, T, headSize)

        return output;
    }
}

/// <summary>
/// The Multi-head attention mechanism involves running the attention process multiple times in parallel 
/// and aggregating the results, allowing the model to pay attention to different positions at the same time.
/// This class consists of multiple 'Head's and aggregates their outputs, followed by a linear projection.
/// </summary>
/// Timestamp: 1:22:20
public sealed class MultiHeadAttention : torch.nn.Module
{
    // List of attention heads. Each head operates on the same input independently and produces its own output.
    private readonly ModuleList<Head> _heads;

    // Linear transformation applied to the concatenated outputs of all heads, to compress them back to the original input size.
    private readonly Linear _proj;

    // Dropout layer for regularization, applied after the linear transformation.
    private readonly Dropout _dropout;

    public MultiHeadAttention(string name) : base(name)
    {
        _heads = new ModuleList<Head>();
        for (int i = 0; i < Settings.NHead; i++)
        {
            // Each head will have its own set of parameters (key, query, value transformations).
            _heads.Add(new Head($"head_{i}")); 
        }
        register_module("heads", _heads);

        _proj = torch.nn.Linear(Settings.HeadSize * Settings.NHead, Settings.NEmbed);
        register_module("proj", _proj);

        _dropout = torch.nn.Dropout(Settings.DropoutValue);
        register_module("dropout", _dropout);
    }

    /// <param name="x">Input tensor of shape (batch_size, sequence_length, Settings.NEmbed).</param>
    /// <returns>Processed tensor of shape (batch_size, sequence_length, Settings.NEmbed).</returns>
    public Tensor Forward(Tensor x)
    {
        List<Tensor> outputs = new List<Tensor>();

        // For each head, run the attention mechanism and store the result in 'outputs'.
        foreach (var head in _heads)
        {
            outputs.Add(head.Forward(x));
        }

        // Concatenate the outputs from all heads along the last dimension.
        // This essentially stacks the outputs of all heads side by side.
        Tensor outTensor = torch.cat(outputs, dim: -1);

        // Apply the linear transformation followed by dropout to the concatenated tensor.
        // The linear transformation compresses the concatenated outputs back to the size of the original input tensor.
        outTensor = _dropout.forward(_proj.forward(outTensor));

        return outTensor;
    }
}

/// <summary>
/// The 'FeedForward' class represents a position-wise feed-forward network (FFN) used in Transformer architectures.
/// It consists of two linear transformations with a ReLU activation in between.
/// This network is applied to each position separately and identically, meaning it doesn't depend on other positions in the sequence.
/// </summary>
/// Timestamp: 1:25:00
public sealed class FeedForward : torch.nn.Module
{
    // The sequential container representing the position-wise feed-forward network.
    private Sequential _net;

    public FeedForward(string name) : base(name)
    {
        // 1. Expanding the input to '4 * Settings.NEmbed' dimensions.
        // 2. Apply ReLU activation
        // 3. Compress it back to 'Settings.NEmbed' dimensions.
        // 4. Dropout layer.
        _net = torch.nn.Sequential(
            ("linear1", torch.nn.Linear(Settings.NEmbed, 4 * Settings.NEmbed)),
            ("relu", torch.nn.ReLU()),
            ("linear2", torch.nn.Linear(4 * Settings.NEmbed, Settings.NEmbed)),
            ("dropout", torch.nn.Dropout(Settings.DropoutValue))
        );
        register_module("net", _net);
    }

    public Tensor Forward(Tensor x)
    {
        return _net.forward(x);
    }
}

/// <summary>
/// The 'Block' class represents an individual unit within the Transformer model architecture.
/// In the Transformer's encoder and decoder stacks, a block refers to a set of layers that 
/// encompass a multi-head attention mechanism followed by a position-wise feed-forward network.
/// These mechanisms are complemented by residual connections and layer normalization stages.
/// Multiple such blocks are stacked to form the complete Transformer encoder or decoder.
/// </summary>
/// Timestamp: 1:26:30
public sealed class Block : torch.nn.Module
{
    private readonly MultiHeadAttention _sa;
    private readonly FeedForward _ffwd;

    // Layer normalization for the input to the self-attention mechanism.
    private readonly LayerNorm _ln1;

    // Layer normalization for the input to the feed-forward network.
    private readonly LayerNorm _ln2;
    public Block(string name) : base(name)
    {
        _sa = new MultiHeadAttention($"sa_{name}"); // replace `Settings.DropoutValue` with the appropriate dropout
        register_module("sa", _sa);

        _ffwd = new FeedForward($"ffwd_{name}"); // replace `Settings.DropoutValue` with the appropriate dropout
        register_module("ffwd", _ffwd);

        _ln1 = torch.nn.LayerNorm(Settings.NEmbed);
        register_module("ln1", _ln1);

        _ln2 = torch.nn.LayerNorm(Settings.NEmbed);
        register_module("ln2", _ln2);
    }

    public Tensor Forward(Tensor x)
    {
        // Process the input through layer normalization and then the self-attention mechanism.
        // Add the output of this to the original input (residual connection).
        x += _sa.Forward(_ln1.forward(x));

        // Process the updated input through another layer normalization and then the feed-forward network.
        // Add the output of this to the updated input (another residual connection).
        x += _ffwd.Forward(_ln2.forward(x));
        return x;
    }
}

public sealed class GptLanguageModel : torch.nn.Module
{
    // Token embedding table, to transform token IDs into embeddings
    private readonly Embedding _tokenEmbeddingTable;

    // Position embedding table, to provide a sense of position/order to the model
    private readonly Embedding _positionEmbeddingTable;

    // Final layer normalization to stabilize and smoothen the activations
    private readonly LayerNorm _lnF;

    // The linear head that maps the final embedding to the vocabulary size, predicting the next token's probability distribution
    private readonly Linear _lmHead;

    // List of transformer blocks (each containing multi-head attention and feed-forward network)
    private readonly List<Block> _blocksList;

    public GptLanguageModel(string name, long vocabSize) : base(name)
    {
        // Initialize token embeddings from the given vocabulary size and embedding dimension
        _tokenEmbeddingTable = torch.nn.Embedding(vocabSize, Settings.NEmbed);
        register_module("token_embedding_table", _tokenEmbeddingTable);

        // Initialize position embeddings from the sequence length (block size) and embedding dimension
        _positionEmbeddingTable = torch.nn.Embedding(Settings.BlockSize, Settings.NEmbed);
        register_module("position_embedding_table", _positionEmbeddingTable);

        _blocksList = new List<Block>();
        for (int i = 0; i < Settings.NLayer; i++)
        {
            var block = new Block($"block_{i}");
            _blocksList.Add(block);
            register_module($"block_{i}", block);
        }

        _lnF = torch.nn.LayerNorm(Settings.NEmbed);
        register_module("ln_f", _lnF);

        _lmHead = torch.nn.Linear(Settings.NEmbed, vocabSize);
        register_module("lm_head", _lmHead);

        // Apply custom weight initialization method
        apply(_initWeights);
    }

    // Weight initialization method specific to linear and embedding layers for model robustness
    private void _initWeights(torch.nn.Module module)
    {
        if (module is Linear linearLayer)
        {
            // Initialize the weights of the linear layer with a normal distribution
            var newLinearWeight = torch.normal(mean: 0.0, std: 0.02, size: linearLayer.weight!.shape).to(Settings.Device);
            linearLayer.weight = torch.nn.Parameter(newLinearWeight);

            // If the linear layer has a bias term, initialize it with zeros
            if (linearLayer.bias is { } bias)
            {
                var newBias = torch.zeros(bias.shape).to(Settings.Device);
                linearLayer.bias = torch.nn.Parameter(newBias);
            }
        }
        else if (module is Embedding embeddingLayer)
        {
            // Initialize the weights of the embedding layer with a normal distribution
            var newEmbeddingWeight = torch.normal(mean: 0.0, std: 0.02, size: embeddingLayer.weight!.shape).to(Settings.Device);
            embeddingLayer.weight = torch.nn.Parameter(newEmbeddingWeight);
        }
    }

    public (Tensor logits, Tensor? loss) Forward(Tensor idx, Tensor? targets = null)
    {
        // Extract batch size and sequence length from the input tensor
        (long b1,long t1) = (idx.size(0), idx.size(1));

        // Convert token indices into token embeddings
        // Timestamp: 59:00
        Tensor tokEmb = _tokenEmbeddingTable.forward(idx); // (B,T,C)

        // Generate position embeddings
        Tensor posEmb = _positionEmbeddingTable.forward(torch.arange(t1, device: idx.device)); // (T,C)

        // Combine token and position embeddings
        // Timestamp: 1:01:15
        Tensor x = tokEmb + posEmb; // (B,T,C) holds not just the token embeddings but also the positional embeddings

        // Pass the combined embeddings through each transformer block
        foreach (var block in _blocksList)
        {
            x = block.Forward(x);
        }

        // Apply the final layer normalization
        x = _lnF.forward(x);

        // Compute the logits using the linear head
        Tensor logits = _lmHead.forward(x);

        // If targets are provided, reshape the logits and compute the cross-entropy loss
        Tensor? loss = null;
        if (targets is not null)
        {
            (long b2,long t2,long c2) = (logits.size(0), logits.size(1), logits.size(2));
            logits = logits.view(b2 * t2, c2);
            targets = targets.view(b2 * t2);
            loss = torch.nn.functional.cross_entropy(logits, targets);
        }

        return (logits, loss);
    }

    public IEnumerable<short> Generate(Tensor allGeneratedTokens, int maxNewTokens)
    {
        using var noGrad = torch.no_grad();
        eval();
        // in video max new tokens was the context window but that was slowing things down a lot for me
        const int contextWindow = 200;

        for (int i = 0; i < maxNewTokens; i++)
        {
            long start = Math.Max(0, allGeneratedTokens.size(1) - contextWindow); // Gets the first token 

            // Extract the relevant section of the tensor for the current context
            Tensor idxCond = allGeneratedTokens.narrow(1, start, allGeneratedTokens.size(1) - start);

            // Compute the logits for the selected context
            (Tensor logits, _) = Forward(idxCond);

            // Extract the logits corresponding to the last token
            logits = logits.select(1, -1);

            // Compute the probabilities for each token in the vocabulary
            Tensor probs = torch.nn.functional.softmax(logits, -1);

            // Randomly sample a new token based on the computed probabilities
            Tensor newlyGeneratedToken = torch.multinomial(probs, 1);

            // Append the newly generated token to the context
            allGeneratedTokens = torch.cat(new[] { allGeneratedTokens, newlyGeneratedToken }, 1);
            yield return (short) newlyGeneratedToken.item<long>();
        }
        train();
    }

    public void GenerateAndPrint(TokenEncoder tokenEncoder, int maxNewTokens)
    {
        LibLog.LogInfo("\n====Generating:====\n");

        // Timestamp: 32:15
        Tensor context = torch.zeros(new long[] { 1, 1 }, dtype: torch.ScalarType.Int64).to(Settings.Device);
        foreach (var token in Generate(context, maxNewTokens))
        {
            LibLog.LogInfo(tokenEncoder.Decode(token).ToString());
        }

        LibLog.LogInfo("\n\n====Generation Completed====\n");
    }
}
