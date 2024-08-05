using Bridge.State;
using Google.FlatBuffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using rlbot.flat;
using RLBotCS.Conversion;

namespace RLBotCSTests;

[TestClass]
public class TestGameState
{
    [TestMethod]
    public void Test()
    {
        var packet = new GameState();

        FlatBufferBuilder builder = new(1024);
        var offset = GameTickPacket.Pack(builder, packet.ToFlatBuffers());
        builder.Finish(offset.Value);
    }
}
