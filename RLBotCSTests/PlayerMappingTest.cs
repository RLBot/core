using Bridge.Models.Message;
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
        uint desiredIndex = 2;
        ushort actorId = 2398;
        ushort commandId = 9855;

        var spawnTracker = new SpawnTracker()
        {
            SpawnId = spawnId,
            CommandId = commandId,
            DesiredPlayerIndex = desiredIndex,
            IsBot = true
        };

        _playerMapping.AddPendingSpawn(spawnTracker);

        var carSpawn = new CarSpawn() { ActorId = actorId, CommandId = commandId, };
        var metadata = _playerMapping.ApplyCarSpawn(carSpawn);

        Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));
        Assert.IsTrue(metadata.IsBot);
        Assert.IsTrue(!metadata.IsCustomBot);

        var metadata2 = _playerMapping.ApplyCarSpawn(
            new CarSpawn() { ActorId = 111, CommandId = 222, }
        );
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
