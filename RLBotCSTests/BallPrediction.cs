using Bridge.Models.Phys;
using Bridge.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.ManagerTools;

namespace RLBotCSTests
{
    [TestClass]
    public class BallPrediction
    {
        [TestMethod]
        public void TestBallPred()
        {
            BallPredictor.SetMode(PredictionMode.Standard);

            var packet = new GameState();
            BallPredictor.Generate(PredictionMode.Standard, 1, packet.Ball);

            packet.Ball.Physics = new Physics(
                new Vector3(0, 0, 100),
                new Vector3(600, 600, 100),
                new Vector3(1, 2, 0.5F),
                new Rotator(0, 0, 0)
            );

            var ballPred = BallPredictor.Generate(PredictionMode.Standard, 1, packet.Ball);

            int numSlices = 8 * 120;
            Assert.AreEqual(numSlices, ballPred.Slices.Count);
            Assert.IsTrue(ballPred.Slices[numSlices - 1].GameSeconds > 8.9999);
        }
    }
}
