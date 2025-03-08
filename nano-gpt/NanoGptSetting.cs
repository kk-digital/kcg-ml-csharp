using Shared;
using TorchSharp;

namespace NanoGptSetting
{
    // Exact settings from video in comments, will likely cause your GPU to run out of 
    // memory if you try with CUDA
    public static class Settings
    {
        /// <summary>
        /// Controls whether to train the model or go straight to generating
        /// </summary>
        public string TrainFile = "kcg-ml-csharp/nano-gpt/input.txt";
        public string NanoGptWeightDataDir = "C:/Models/";
        public static Mode Mode { get; set; } = Mode.Train;
        public static string SaveLocation(int vocabSize) => $"{NanoGptWeightDataDir}NanoGpt_{SettingsKey}_{vocabSize}.dat";
        public static string SettingsKey => $"{Device.type}_{NEmbed}_{NHead}_{NLayer}";

        /// <summary>
        /// Controls whether to generate tokens at each evaluations internal, in addition
        /// to evaluating the loss.
        /// </summary>
        public static bool GenerateOnEvaluate { get; set; } = true;
        /// <summary>
        /// Max number of times the models weights will be updated.
        /// Also how many forward passes of the model to perform.
        /// </summary>
        public static int MaxIterations { get; set; } = 20000;
        /// <summary>
        /// Controls how often to evaluate the model
        /// </summary>
        public static int EvalInterval { get; set; } = 250; // Video 750
        /// <summary>
        /// Controls how many times to calculate the loss when evaluating the model.
        /// More eval iterations gives a more accurate estimate of the models performance.
        /// </summary>
        public static int EvalIterations { get; set; } = 100; // Video 200
        /// <summary>
        /// Controls where the tensors live
        /// </summary>

        // You will need a good GPU to train this model, not all of us have A100s
        #if USE_GPU
        public static torch.Device Device = torch.cuda.is_available() ? torch.CUDA : torch.CPU; // Change to CUDA if you have good gpu and install CUDA driver in shared csproj by uncommenting
        #else
        public static torch.Device Device = torch.cuda.is_available() ? torch.CPU : torch.CPU; // Change to CUDA if you have good gpu and install CUDA driver in shared csproj by uncommenting
        #endif

        /// <summary>
        /// The number of samples processed in one iteration of model training.
        /// A larger batch size requires more memory but can lead to faster convergence.
        /// </summary>
        public const int BatchSize = 64;
        /// <summary>
        /// The number of tokens in each batch.
        /// Higher block size increases memory usage.
        /// </summary>
        public const int BlockSize = 256;
        /// <summary>
        /// The learning rate for the optimizer.
        /// This controls the size of the updates to the model's weights during training.
        /// </summary>
        public const double LearningRate = 3e-4;
        /// <summary>
        /// The dropout rate applied to layers during training to prevent overfitting.
        /// Dropout randomly sets input units to 0 at each update during training time,
        /// which helps to regularize the model.
        /// </summary>
        public const double DropoutValue = 0.2;
        /// <summary>
        /// The size of the embedding layer.
        /// This represents the size of the vectors used to
        /// encode the tokens into continuous vectors before
        /// feeding them into the model.
        /// </summary>
        public const int NEmbed = 384; 
        /// <summary>
        /// The number of attention heads in the transformer model.
        /// Multiple heads allow the model to jointly attend to characters
        /// at different positions in the input.
        /// </summary>
        public const int NHead = 6; 
        /// <summary>
        /// Size/dimension of each head's output. The division ensures each head processes a segment of 
        /// the embedding dimension
        /// </summary>
        public const int HeadSize = NEmbed / NHead;
        /// <summary>
        /// The number of transformer layers in the model.
        /// Each layer consists of a multi-head attention mechanism
        /// followed by a feed-forward network.
        /// More layers can increase the model's capacity to learn complex patterns.
        /// </summary>
        public const int NLayer = 6;
    }
}