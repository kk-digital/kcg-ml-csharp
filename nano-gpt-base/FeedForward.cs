using Shared;
using TorchSharp;
using TorchSharp.Modules;
using Tensor = TorchSharp.torch.Tensor;
using Settings = NanoGptBaseSetting.Settings;
using LogUtility;

namespace TransformerFFN
{
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
}