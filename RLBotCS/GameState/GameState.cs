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
                        team = carSpawn.team
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

                    switch (stateTransition.gameState) {
                        case GameStateType.Inactive:
                            gameTickPacket.isMatchEnded = true;
                            gameTickPacket.isRoundActive = false;
                            gameTickPacket.isKickoffPause = false;
                            break;
                        case GameStateType.Countdown:
                            gameTickPacket.isMatchEnded = false;
                            gameTickPacket.isKickoffPause = true;
                            gameTickPacket.isRoundActive = false;
                            break;
                        case GameStateType.Kickoff:
                            gameTickPacket.isMatchEnded = false;
                            gameTickPacket.isKickoffPause = true;
                            gameTickPacket.isRoundActive = true;
                            break;
                        case GameStateType.Active:
                            gameTickPacket.isMatchEnded = false;
                            gameTickPacket.isKickoffPause = false;
                            gameTickPacket.isRoundActive = true;
                            break;
                        case GameStateType.GoalScored:
                            gameTickPacket.isMatchEnded = false;
                            gameTickPacket.isKickoffPause = false;
                            gameTickPacket.isRoundActive = false;
                            break;
                        case GameStateType.Replay:
                            gameTickPacket.isMatchEnded = false;
                            gameTickPacket.isKickoffPause = false;
                            gameTickPacket.isRoundActive = false;
                            break;
                        case GameStateType.Paused:
                            gameTickPacket.isRoundActive = false;
                            break;
                        case GameStateType.Ended:
                            gameTickPacket.isMatchEnded = true;
                            gameTickPacket.isKickoffPause = false;
                            gameTickPacket.isRoundActive = false;
                            break;
                    }
                }
                // TODO: lots more message handlers.
            }
        }
    }
}
