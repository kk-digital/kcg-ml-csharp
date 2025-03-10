using Shared;
using TorchSharp;
using TorchSharp.Modules;
using Tensor = TorchSharp.torch.Tensor;
using Settings = NanoGptSetting.Settings;
using BaseSettings = NanoGptBaseSetting.Settings;
using LogUtility;
using Block = TransformerBlock.Block;
// ReSharper disable All
#pragma warning disable IDE0059

namespace Gpt;

// Translated from python code written by Andrej Karpathy https://www.youtube.com/watch?v=kCc8FmEb1nY
// Comments are a mix of my comments, comments from the video, and GPT-4
// Timestamps of video in comments

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
        _tokenEmbeddingTable = torch.nn.Embedding(vocabSize, BaseSettings.NEmbed);
        register_module("token_embedding_table", _tokenEmbeddingTable);

        // Initialize position embeddings from the sequence length (block size) and embedding dimension
        _positionEmbeddingTable = torch.nn.Embedding(BaseSettings.BlockSize, BaseSettings.NEmbed);
        register_module("position_embedding_table", _positionEmbeddingTable);

        _blocksList = new List<Block>();
        for (int i = 0; i < BaseSettings.NLayer; i++)
        {
            var block = new Block($"block_{i}");
            _blocksList.Add(block);
            register_module($"block_{i}", block);
        }

        _lnF = torch.nn.LayerNorm(BaseSettings.NEmbed);
        register_module("ln_f", _lnF);

        _lmHead = torch.nn.Linear(BaseSettings.NEmbed, vocabSize);
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
