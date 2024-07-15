using System.Diagnostics;
using Bridge.Models.Phys;
using Bridge.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.ManagerTools;

namespace RLBotCSTests
{
    [TestClass]
    public class BallPrediction
    {
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestMethod]
        public void TestBallPred()
        {
            BallPredictor.SetMode(PredictionMode.Standard);

            var packet = new GameState();
            packet.Balls[12345] = new();

            BallPredictor.Generate(PredictionMode.Standard, 1, packet.Balls[12345]);

            packet.Balls[12345].Physics = new Physics(
                new Vector3(0, 0, 1.1f * 91.25f),
                new Vector3(600, 1550, 0),
                new Vector3(0, 0, 0),
                new Rotator(0, 0, 0)
            );

            var ballPred = BallPredictor.Generate(PredictionMode.Standard, 1, packet.Balls[12345]);

            int numSlices = 6 * 120;
            Assert.AreEqual(numSlices, ballPred.Slices.Count);
            Assert.IsTrue(ballPred.Slices[numSlices - 1].GameSeconds > 5.9999);

            // comment out to see results of the below test
            // dotnet test -c "Release" for best results
            return;

            Stopwatch stopWatch = new Stopwatch();

            int numIterations = 20_000;
            for (int i = 0; i < numIterations; i++)
            {
                packet.Balls[12345].Physics = new Physics(
                    new Vector3(0, 0, 1.1f * 91.25f),
                    new Vector3(600, 1550, 0),
                    new Vector3(0, 0, 0),
                    new Rotator(0, 0, 0)
                );

                stopWatch.Start();
                BallPredictor.Generate(PredictionMode.Standard, 1, packet.Balls[12345]);
                stopWatch.Stop();
            }

            float averageTime = (float)stopWatch.ElapsedMilliseconds / numIterations;
            TestContext.WriteLine("Average time to generate ball prediction: " + averageTime + "ms");

            // makes the above result print out
            Assert.IsTrue(false);
        }
    }
}
