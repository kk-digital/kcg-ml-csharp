using Shared;
using TorchSharp;
using TorchSharp.Modules;
using Tensor = TorchSharp.torch.Tensor;
using Settings = NanoGptBaseSetting.Settings;
using LogUtility;

namespace TransformerHead
{
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
}