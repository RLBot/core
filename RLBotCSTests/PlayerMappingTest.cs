using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotSecret.Models.Message;
using RLBotSecret.State;

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

            var carSpawn = new CarSpawn()
            {
                ActorId = actorId,
                CommandId = commandId,
                Name = "MyBot",
                Team = 1
            };
            _playerMapping.ApplyCarSpawn(carSpawn);

            Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));

            _playerMapping.ApplyCarSpawn(
                new CarSpawn()
                {
                    ActorId = 111,
                    CommandId = 222,
                    Name = "SomeHuman",
                    Team = 0
                }
            );

            Assert.AreEqual(0u, _playerMapping.PlayerIndexFromActorId(111));
            Assert.AreEqual(desiredIndex, _playerMapping.PlayerIndexFromActorId(actorId));
            Console.Write("Good");
        }
    }
}
