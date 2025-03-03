using NUnit.Framework;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace TorchSharpTests
{
    [TestFixture]
    public class TorchSharpModelTests
    {
        public Sequential Seq;
        public torch.optim.Optimizer Optimizer;

        [SetUp]
        public void Setup()
        {
            if (!torch.cuda.is_available()) return; // Skip if no GPU
            
            Linear lin1 = Linear(1000, 100);
            Linear lin2 = Linear(100, 10);
            Seq = Sequential(("lin1", lin1), ("relu1", ReLU()), ("drop1", Dropout(0.1)), ("lin2", lin2));
            Optimizer = torch.optim.Adam(Seq.parameters());
        }

        [Test]
        public void ForwardPass_ShouldReturnCorrectShape()
        {
            if (!torch.cuda.is_available()) return; // Skip if no GPU
            
            using torch.Tensor x = torch.randn(64, 1000);
            using torch.Tensor eval = Seq.forward(x);
            
            Assert.AreEqual(new long[] { 64, 10 }, eval.shape);
        }
        
        [Test]
        public void LossComputation_ShouldNotThrowException()
        {
            if (!torch.cuda.is_available()) return; // Skip if no GPU
            
            using torch.Tensor x = torch.randn(64, 1000);
            using torch.Tensor y = torch.randn(64, 10);
            using torch.Tensor eval = Seq.forward(x);
            using torch.Tensor output = functional.mse_loss(eval, y, Reduction.Sum);

            Assert.DoesNotThrow(() => output.backward());
        }

        [Test]
        public void OptimizerStep_ShouldUpdateParameters()
        {
            if (!torch.cuda.is_available()) return; // Skip if no GPU
            
            using torch.Tensor x = torch.randn(64, 1000);
            using torch.Tensor y = torch.randn(64, 10);
            using torch.Tensor eval = Seq.forward(x);
            using torch.Tensor output = functional.mse_loss(eval, y, Reduction.Sum);

            Optimizer.zero_grad();
            output.backward();

            var initialParams = Seq.parameters().ToList()[0].clone();
            Optimizer.step();
            
            var updatedParams = Seq.parameters().ToList()[0];
            Assert.IsFalse(torch.allclose(initialParams, updatedParams));
        }
    }
}