using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBotCS.GameState;
using RLBotModels.Message;

namespace RLBotCSTests
{
    [TestClass]
    public class PlayerMappingTest
    {
        private PlayerMapping playerMapping;

        [TestInitialize]
        public void init()
        {
            playerMapping = new PlayerMapping();
        }

        [TestMethod]
        public void TestSpawnProcess()
        {
            int spawnId = 2398249;
            int desiredIndex = 2;
            ushort actorId = 2398;
            ushort commandId = 9855;

            var spawnTracker = new SpawnTracker()
            {
                spawnId = spawnId,
                commandId = commandId,
                desiredPlayerIndex = desiredIndex,
                isCustomBot = true
            };

            playerMapping.addPendingSpawn(spawnTracker);

            var carSpawn = new CarSpawn()
            {
                actorId = actorId,
                commandId = commandId,
                name = "MyBot",
                team = 1
            };
            playerMapping.applyCarSpawn(carSpawn);

            Assert.AreEqual(desiredIndex, playerMapping.PlayerIndexFromActorId(actorId));

            playerMapping.applyCarSpawn(new CarSpawn()
            {
                actorId = 111,
                commandId = 222,
                name = "SomeHuman",
                team = 0
            });


            Assert.AreEqual(0, playerMapping.PlayerIndexFromActorId(111));
            Assert.AreEqual(desiredIndex, playerMapping.PlayerIndexFromActorId(actorId));
        }
    }
}