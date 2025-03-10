using Shared;
using TorchSharp;
using TorchSharp.Modules;
using Tensor = TorchSharp.torch.Tensor;
using Settings = NanoGptBaseSetting.Settings;
using LogUtility;
using MultiHeadAttention = TransformerMultiHeadAttn.MultiHeadAttention;
using FeedForward = TransformerFFN.FeedForward;

namespace TransformerBlock
{
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
}