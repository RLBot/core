using Bridge.Models.Phys;
using Bridge.Packet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.ManagerTools;
using System;

namespace RLBotCSTests
{
    [TestClass]
    public class BallPrediction
    {
        [TestMethod]
        public void TestBallPred()
        {
            BallPredictor.SetMode(PredictionMode.Standard);

            var currentBall = new Ball()
            {
                Physics = new Physics()
                {
                    location = new Vector3
                    {
                        x = 0,
                        y = 0,
                        z = 100
                    },
                    velocity = new Vector3
                    {
                        x = 600,
                        y = 600,
                        z = 100
                    },
                    angularVelocity = new Vector3
                    {
                        x = 1,
                        y = 2,
                        z = 0.5F,
                    }
                }
            };

            var ballPred = BallPredictor.Generate(PredictionMode.Standard, 1, currentBall);

            int numSlices = 8 * 120;
            Assert.AreEqual(numSlices, ballPred.Slices.Count);
            Assert.IsTrue(ballPred.Slices[numSlices - 1].GameSeconds > 8.9999);
        }
    }
}
