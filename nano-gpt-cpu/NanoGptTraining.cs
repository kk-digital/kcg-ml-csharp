using Shared;
using System.Diagnostics;
using TorchSharp;
using TorchSharp.Modules;
using Tensor = TorchSharp.torch.Tensor;
using NanoGptArgsSettings;
using Settings = NanoGptSetting.Settings;
using BaseSettings = NanoGptBaseSetting.Settings;
using Gpt;
using LogUtility;
using FileLib;
using UtilityIO;

namespace NanoGptTraining
{
    public class Training
    {
        public static void Train(TokenEncoder tokenEncoder, string text, int vocabSize, ArgsSettings argsSettings)
        {
            List<short> encoded = tokenEncoder.Encode(text);

            // One dimensional tensor of all the encoded tokens [ 0, 32, 45,... ]
            Tensor data = torch.tensor(encoded, torch.ScalarType.Int64);

            long numberToTrain = (long)(data.shape[0] * 0.9);
            long numberToTest = data.shape[0] - numberToTrain;

            // Split the data into training and testing
            // 90% for training, 10% for testing
            Tensor trainData = data[..(int)numberToTrain];
            Tensor testData = data[(int)numberToTrain..];

            LibLog.LogInfo($"{numberToTrain}");
            LibLog.LogInfo($"{numberToTest}");

            DataSampler dataSampler = new DataSampler(trainData, testData);
            GptLanguageModel model = new GptLanguageModel("My_Language_Model", vocabSize).to(Settings.Device);
            if (FileUtils.FileExists(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey)))
            {
                model.load(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey));
            }

            // Timestamp: 35:15
            AdamW optimizer = torch.optim.AdamW(model.parameters(), lr: BaseSettings.LearningRate);

            var parameterCount = model.parameters().Sum(p => p.numel());
            LibLog.LogInfo($"Parameters Count: {parameterCount}");

            // just to time the length of an iteration
            Stopwatch stopwatch = new Stopwatch();

            float[] lowestEval = new [] { float.MaxValue, float.MaxValue };
            int patienceCounter = 0;
            for (int i = 0; i < BaseSettings.MaxIterations; i++)
            {
                // Check if it's time to evaluate the model based on the evaluation interval setting.
                // This is done periodically and not at every single training step to save compute time.
                if (i != 0 && i % BaseSettings.EvalInterval == 0)
                {
                    // Calculate the loss on the training and test data sets
                    float[] losses = EstimateLoss(model, dataSampler);
                    LibLog.LogInfo($"step {i}: train loss {losses[0]:F4}, val loss {losses[1]:F4}");

                    // If the current losses are the lowest observed, update the best model checkpoint.
                    if (losses[0] < lowestEval[0] && losses[1] < lowestEval[1])
                    {
                        lowestEval = losses;
                        var directory = PathUtils.GetDirectoryName(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey));
                        if (!FileUtils.DirectoryExists(directory))
                        {
                            FileUtils.CreateDirectory(directory!);
                        }
                        model.save(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey));
                        patienceCounter = 0;
                    }
                    // Allow the model some leeway so it can explore different
                    // pathways. Sometimes you have to take 1 step backwards
                    // to take 2 steps forwards.
                    else if (patienceCounter < 4)
                    {
                        patienceCounter++;
                    }
                    // If the model still hasn't improved, revert to the previous best model.
                    else
                    {
                        if (FileUtils.FileExists(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey)))
                        {
                            model.load(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey));
                            patienceCounter = 0;
                        }
                    }

                    if (BaseSettings.GenerateOnEvaluate)
                    {
                        model.GenerateAndPrint(tokenEncoder, maxNewTokens: 200);
                    }
                }
                stopwatch.Restart();

                // Get random input blocks from the train data
                // with their respective targets. Targets
                // are just the input tensors offset by 1 index
                // to the right, they represent what
                // is supposed to come next.
                (Tensor inputs, Tensor targets) = dataSampler.RandomSamples(DataType.Train, BaseSettings.BatchSize, BaseSettings.BlockSize, Settings.Device);

                // Pass the 'inputs' through the GPT model to obtain predictions ('logits') and calculate the loss with respect to 'targets'.
                // The 'logits' tensor contains raw prediction values for each token in the vocabulary, while 'loss' represents the model's error.
                (Tensor logits, Tensor? loss) = model.Forward(inputs, targets);

                // Reset gradients accumulated in the optimizer from the previous iteration.
                optimizer.zero_grad();

                // Backpropagate the error: Compute gradients of the loss with respect to model parameters.
                // This will affect the weights and biases in every tensor in the computation graph leading
                // to the calculation of loss, which is everything because we just did a forward pass of the
                // whole model. All the embedding tables, linear layers, layer norms, etc. in all the modules
                // and sub-modules will be updated.
                // Gradients are computed using derivates and chain rule.
                loss?.backward();

                // Update the model's weights based on computed gradients.
                optimizer.step();

                stopwatch.Stop();
                LibLog.LogInfo($"step {i}: iteration time milliseconds: {stopwatch.ElapsedMilliseconds:F0}");
            }

            // Timestamp: 32:15
            model.GenerateAndPrint(tokenEncoder, maxNewTokens: 500);
            model.save(argsSettings.SaveLocation(vocabSize, Settings.SettingsKey));
        }


        /// <summary>
        /// Estimates the loss of the model across different data types (Train, Test).
        /// Used to evaluate the model's performance by calculating the average loss over a set number of iterations.
        /// Gradient computation is temporarily disabled to optimize memory usage and computation time during this evaluation phase.
        /// </summary>
        // Timestamp: 40:00
        private static float[] EstimateLoss(GptLanguageModel model, DataSampler dataSampler)
        {
            using var noGrad = torch.no_grad();
            var dataTypes = Enum.GetValues<DataType>();
            float[] results = new float[dataTypes.Length];
            model.eval();
            foreach (var dataType in dataTypes)
            {
                var losses = torch.zeros(BaseSettings.EvalIterations);
                for (int k = 0; k < BaseSettings.EvalIterations - 1; k++)
                {
                    (Tensor inputs, Tensor targets) = dataSampler.RandomSamples(dataType, BaseSettings.BatchSize, BaseSettings.BlockSize, Settings.Device);
                    (Tensor logits, Tensor? loss) = model.Forward(inputs, targets);
                    losses[k] = loss!.item<float>();
                }
                results[(int)dataType] = losses.mean().item<float>();
            }
            model.train();
            return results;
        }

    }
}