using RLBotCS.RLBotPacket;
using RLBotModels.Message;

namespace RLBotCS.GameState
{
    internal class GameState
    {
        public GameTickPacket gameTickPacket = new();

        public PlayerMapping playerMapping = new();

        public void applyMessage(MessageBundle messageBundle)
        {
            foreach (var message in messageBundle.messages)
            {
                if (message is CarSpawn carSpawn)
                {
                    var metadata = playerMapping.applyCarSpawn((CarSpawn)message);
                    gameTickPacket.gameCars[metadata.playerIndex] = new GameCar()
                    {
                        name = carSpawn.name,
                        isBot = metadata.isBot,
                        hitbox = carSpawn.hitbox.dimensions,
                        hitboxOffset = carSpawn.hitbox.offset,
                        team = carSpawn.team,
                        spawnId = metadata.spawnId.HasValue ? metadata.spawnId.Value : 0,
                    };
                }
                else if (message is ActorDespawn despawn)
                {
                    var actorId = despawn.actorId;
                    var playerMetadata = playerMapping.tryRemoveActorId(actorId);
                    if (playerMetadata != null)
                    {
                        gameTickPacket.gameCars.Remove(playerMetadata.playerIndex);
                    }
                }
                else if (message is PhysicsUpdate physicsUpdate)
                {
                    foreach (var carUpdate in physicsUpdate.carUpdates)
                    {
                        var actorId = carUpdate.Key;
                        var carPhysics = carUpdate.Value;
                        var playerIndex = playerMapping.PlayerIndexFromActorId(actorId);
                        if (playerIndex.HasValue)
                        {
                            var car = gameTickPacket.gameCars[playerIndex.Value];
                            car.physics = carPhysics.physics;
                            // TODO: car.jumped etc.
                        }
                    }
                    if (physicsUpdate.ballUpdate.HasValue)
                    {
                        gameTickPacket.ball.physics = physicsUpdate.ballUpdate.Value;
                    }
                }
                else if (message is PlayerBoostUpdate boostUpdate)
                {
                    var playerIndex = playerMapping.PlayerIndexFromActorId(boostUpdate.actorId);
                    if (playerIndex.HasValue)
                    {
                        var car = gameTickPacket.gameCars[playerIndex.Value];
                        car.boost = boostUpdate.boostRemaining;
                    }
                }
                else if (message is GameStateTransition stateTransition)
                {
                    gameTickPacket.isOvertime = stateTransition.isOvertime;
                    gameTickPacket.gameState = stateTransition.gameState;
                }
                // TODO: lots more message handlers.
            }
        }
    }
}
