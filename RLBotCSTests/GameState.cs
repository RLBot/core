using Bridge.State;
using Google.FlatBuffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.Conversion;

namespace RLBotCSTests;

[TestClass]
public class TestGameState
{
    [TestMethod]
    public void Test()
    {
        var packet = new GameState();

        FlatBufferBuilder build = new(1024);
        packet.ToFlatBuffers(build);
    }
}
