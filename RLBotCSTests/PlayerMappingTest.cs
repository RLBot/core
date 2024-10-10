using Bridge.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RLBotCSTests;

[TestClass]
public class PlayerMappingTest
{
    private PlayerMapping _playerMapping;

    [TestInitialize]
    public void Init()
    {
        _playerMapping = new PlayerMapping();
    }

    [TestMethod]
    public void TestSpawnProcess()
    {
        int spawnId = 2398249;
        string agentId = "dev/abot";
        uint desiredIndex = 2;
        ushort actorId = 2398;
        ushort commandId = 9855;

        var spawnTracker = new SpawnTracker()
        {
            SpawnId = spawnId,
            AgentId = agentId,
            CommandId = commandId,
            DesiredPlayerIndex = desiredIndex,
            IsBot = true
        };

        _playerMapping.AddPendingSpawn(spawnTracker);

        var metadata = _playerMapping.ApplyCarSpawn(actorId, commandId);

        Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));
        Assert.AreEqual(spawnId, metadata.SpawnId);
        Assert.AreEqual(agentId, metadata.AgentId);
        Assert.IsTrue(metadata.IsBot);
        Assert.IsTrue(!metadata.IsCustomBot);

        var metadata2 = _playerMapping.ApplyCarSpawn(111, 222);
        uint? index = _playerMapping.PlayerIndexFromActorId(111);

        Assert.AreEqual(0u, _playerMapping.PlayerIndexFromActorId(111));
        Assert.IsNotNull(index);
        Assert.AreEqual(index, 0u);
        Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));
        Assert.IsTrue(!metadata2.IsBot);
        Assert.IsTrue(!metadata2.IsCustomBot);

        uint? index2 = _playerMapping.PlayerIndexFromActorId(456);

        Assert.IsNull(index2);
        Assert.AreNotEqual(index2, 0u);
    }
}
