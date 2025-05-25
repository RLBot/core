using Bridge.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RLBotCSTests;

[TestClass]
public class PlayerMappingTest
{
    private PlayerMapping? _playerMapping;

    [TestInitialize]
    public void Init()
    {
        _playerMapping = new PlayerMapping();
    }

    [TestMethod]
    public void TestSpawnProcess()
    {
        int playerId = 2398249;
        string agentId = "dev/abot";
        uint desiredIndex = 2;
        ushort actorId = 2398;
        ushort commandId = 9855;

        var spawnTracker = new SpawnTracker()
        {
            PlayerId = playerId,
            AgentId = agentId,
            CommandId = commandId,
            DesiredPlayerIndex = desiredIndex,
            IsBot = true,
        };

        // add pending spawn
        _playerMapping!.AddPendingSpawn(spawnTracker);

        // apply car spawn from known player
        var metadata = _playerMapping.ApplyCarSpawn(actorId, commandId);

        Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));
        Assert.AreEqual(playerId, metadata.PlayerId);
        Assert.AreEqual(agentId, metadata.AgentId);
        Assert.IsTrue(metadata.IsBot);
        Assert.IsTrue(!metadata.IsCustomBot);

        // apply car spawn from unknown player
        var metadata2 = _playerMapping.ApplyCarSpawn(111, 222);
        uint? index = _playerMapping.PlayerIndexFromActorId(111);

        Assert.AreEqual(0u, _playerMapping.PlayerIndexFromActorId(111));
        Assert.IsNotNull(index);
        Assert.AreEqual(0u, index);
        Assert.AreNotEqual(0, metadata2.PlayerId);
        Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));
        Assert.IsTrue(!metadata2.IsBot);
        Assert.IsTrue(!metadata2.IsCustomBot);

        uint? index2 = _playerMapping.PlayerIndexFromActorId(456);

        Assert.IsNull(index2);
    }
}
