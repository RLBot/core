using System.Diagnostics;
using Bridge.Models.Phys;
using Bridge.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.Conversion;
using RLBotCS.ManagerTools;

namespace RLBotCSTests;

[TestClass]
public class BallPrediction
{
    private TestContext? testContextInstance;

    /// <summary>
    /// Gets or sets the test context which provides
    /// information about and functionality for the current test run.
    /// </summary>
    public TestContext TestContext
    {
        get { return testContextInstance!; }
        set { testContextInstance = value; }
    }

    [TestMethod]
    public void TestBallPred()
    {
        BallPredictor.SetMode(PredictionMode.Standard);

        var packet = new GameState();
        packet.Balls[12345] = new();
        var gTP = packet.ToFlatBuffers();

        BallPredictor.Generate(1, gTP.Balls[0], null, -650f);

        packet.Balls[12345].Physics = new Physics(
            new Vector3(0, 0, 1.1f * 91.25f),
            new Vector3(600, 1550, 0),
            new Vector3(0, 0, 0),
            new Rotator(0, 0, 0)
        );
        var gTP2 = packet.ToFlatBuffers();

        var ballPred = BallPredictor.Generate(1, gTP2.Balls[0], null, -650f);

        int numSlices = 6 * 120;
        Assert.HasCount(numSlices, ballPred.Slices);
        Assert.IsInRange(6.9999, 7.0001, ballPred.Slices[numSlices - 1].GameSeconds);

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
            var gTP3 = packet.ToFlatBuffers();

            stopWatch.Start();
            BallPredictor.Generate(1, gTP3.Balls[0], null, -650f);
            stopWatch.Stop();
        }

        float averageTime = (float)stopWatch.ElapsedMilliseconds / numIterations;
        TestContext.WriteLine(
            "Average time to generate ball prediction: " + averageTime + "ms"
        );

        // makes the above result print out
        Assert.Fail();
    }
}
