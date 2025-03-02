using NUnit.Framework;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace TorchSharpTests
{
    [TestFixture]
    public class TorchSharpModelTests
    {
        private Sequential seq;
        private torch.optim.Optimizer optimizer;

        [SetUp]
        public void Setup()
        {
            Linear lin1 = Linear(1000, 100);
            Linear lin2 = Linear(100, 10);
            seq = Sequential(("lin1", lin1), ("relu1", ReLU()), ("drop1", Dropout(0.1)), ("lin2", lin2));
            optimizer = torch.optim.Adam(seq.parameters());
        }

        [Test]
        public void ForwardPass_ShouldReturnCorrectShape()
        {
            using torch.Tensor x = torch.randn(64, 1000);
            using torch.Tensor eval = seq.forward(x);
            
            Assert.AreEqual(new long[] { 64, 10 }, eval.shape);
        }
        
        [Test]
        public void LossComputation_ShouldNotThrowException()
        {
            using torch.Tensor x = torch.randn(64, 1000);
            using torch.Tensor y = torch.randn(64, 10);
            using torch.Tensor eval = seq.forward(x);
            using torch.Tensor output = functional.mse_loss(eval, y, Reduction.Sum);

            Assert.DoesNotThrow(() => output.backward());
        }

        [Test]
        public void OptimizerStep_ShouldUpdateParameters()
        {
            using torch.Tensor x = torch.randn(64, 1000);
            using torch.Tensor y = torch.randn(64, 10);
            using torch.Tensor eval = seq.forward(x);
            using torch.Tensor output = functional.mse_loss(eval, y, Reduction.Sum);

            optimizer.zero_grad();
            output.backward();

            var initialParams = seq.parameters().ToList()[0].clone();
            optimizer.step();
            
            var updatedParams = seq.parameters().ToList()[0];
            Assert.IsFalse(torch.allclose(initialParams, updatedParams));
        }
    }
}