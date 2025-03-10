using Shared;
using TorchSharp;
using TorchSharp.Modules;
using Tensor = TorchSharp.torch.Tensor;
using Settings = NanoGptBaseSetting.Settings;
using LogUtility;
using Head = TransformerHead.Head;

namespace TransformerMultiHeadAttn
{
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
}