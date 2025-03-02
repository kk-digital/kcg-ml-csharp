using NUnit.Framework;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

[TestFixture]
public class NeuralNetworkTrainingTest
{
    private const int InputSize = 1000;
    private const int HiddenLayerSize = 100;
    private const int OutputSize = 10;
    private const int BatchSize = 64;
    private const int TrainingIterations = 10;
    private const float DropoutProbability = 0.1f;

    [Test]
    public void TestNeuralNetworkTraining()
    {
        Linear inputToHiddenLayer = Linear(InputSize, HiddenLayerSize);
        Linear hiddenToOutputLayer = Linear(HiddenLayerSize, OutputSize);

        Sequential neuralNetworkModel = Sequential(
            ("inputToHiddenLayer", inputToHiddenLayer),
            ("reluActivation", ReLU()),
            ("dropoutLayer", Dropout(DropoutProbability)),
            ("hiddenToOutputLayer", hiddenToOutputLayer)
        );

        using Tensor inputData = randn(BatchSize, InputSize);
        using Tensor targetData = randn(BatchSize, OutputSize);

        optim.Optimizer optimizerAdam = optim.Adam(neuralNetworkModel.parameters());

        float initialLoss = 0.0f;
        float finalLoss = 0.0f;

        for (int iterationIndex = 0; iterationIndex < TrainingIterations; iterationIndex++)
        {
            using Tensor modelOutput = neuralNetworkModel.forward(inputData);

            using Tensor computedLoss = functional.mse_loss(
                modelOutput,
                targetData,
                Reduction.Sum
            );

            if (iterationIndex == 0)
            {
                initialLoss = computedLoss.ToSingle();
            }

            optimizerAdam.zero_grad();
            computedLoss.backward();
            optimizerAdam.step();

            if (iterationIndex == TrainingIterations - 1)
            {
                finalLoss = computedLoss.ToSingle();
            }
        }

        Assert.Less(
            finalLoss,
            initialLoss,
            "The loss did not decrease, indicating the network did not learn."
        );
    }
}
