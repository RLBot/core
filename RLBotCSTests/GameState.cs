using Bridge.Packet;
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
        var offset = GamePacket.Pack(builder, packet.ToFlatBuffers());
        builder.Finish(offset.Value);

        packet.BoostPads.Add(
            0,
            new BoostPadInfo
            {
                SpawnPosition = new Bridge.Models.Phys.Vector3(1, 1, 0),
                IsActive = true
            }
        );

        packet.BoostPads.Add(
            1,
            new BoostPadInfo
            {
                SpawnPosition = new Bridge.Models.Phys.Vector3(0, 0, 0),
                IsActive = true
            }
        );

        packet.BoostPads.Add(
            2,
            new BoostPadInfo
            {
                SpawnPosition = new Bridge.Models.Phys.Vector3(0, 1, 0),
                IsActive = true
            }
        );

        var flatPacket = packet.ToFlatBuffers();
        Assert.AreEqual(3, flatPacket.BoostPads.Count);
    }
}
