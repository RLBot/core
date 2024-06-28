using System;
using Bridge.Models.Message;
using Bridge.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RLBotCSTests
{
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
                IsCustomBot = true
            };

            _playerMapping.AddPendingSpawn(spawnTracker);

            var carSpawn = new CarSpawn() { ActorId = actorId, CommandId = commandId, };
            _playerMapping.ApplyCarSpawn(carSpawn);

            Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));

            _playerMapping.ApplyCarSpawn(new CarSpawn() { ActorId = 111, CommandId = 222, });

            Assert.AreEqual(0u, _playerMapping.PlayerIndexFromActorId(111));
            Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));
            Console.Write("Good");
        }
    }
}
