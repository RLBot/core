using RLBotCS.RLBotPacket;
using RLBotModels.Message;
using RLBotModels.Phys;

namespace RLBotCS.GameState
{
    internal class GameState
    {
        public GameTickPacket gameTickPacket = new();

        public PlayerMapping playerMapping = new();

        public List<BoostPadSpawn> boostPads = new();

        public void applyMessage(MessageBundle messageBundle)
        {
            gameTickPacket.frameNum += messageBundle.physicsTickDelta;
            gameTickPacket.secondsElapsed = gameTickPacket.frameNum / 120f;

            // TODO: account for matches longer than 5 minutes
            if (gameTickPacket.isUnlimitedTime)
            {
                gameTickPacket.gameTimeRemaining = float.MaxValue;
            }
            else
            {
                var max_game_time_seconds = 5 * 60;
                gameTickPacket.gameTimeRemaining = max_game_time_seconds - gameTickPacket.secondsElapsed;
            }

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
                        ProcessCarUpdate(carUpdate);
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
                else if (message is TeamScoreUpdate scoreUpdate)
                {
                    gameTickPacket.teamScores[scoreUpdate.team] = scoreUpdate.score;
                }
                else if (message is MatchInfo matchInfo)
                {
                    boostPads.Clear();
                    gameTickPacket.worldGravityZ = matchInfo.gravity.z;
                    gameTickPacket.frameNum = 0;
                    gameTickPacket.secondsElapsed = 0;
                }
                else if (message is BoostPadSpawn boostPadSpawn)
                {
                    boostPads.Add(boostPadSpawn);
                }
                else if (message is PlayerStatsUpdate playerAccolade)
                {
                    var playerIndex = playerMapping.PlayerIndexFromActorId(playerAccolade.actorId);
                    if (playerIndex.HasValue)
                    {
                        var car = gameTickPacket.gameCars[playerIndex.Value];

                        car.scoreInfo.score = playerAccolade.score;
                        car.scoreInfo.goals = playerAccolade.goals;
                        car.scoreInfo.ownGoals = playerAccolade.ownGoals;
                        car.scoreInfo.assists = playerAccolade.assists;
                        car.scoreInfo.saves = playerAccolade.saves;
                        car.scoreInfo.shots = playerAccolade.shots;
                        car.scoreInfo.demolitions = playerAccolade.demolitions;
                    }
                }
                // TODO: lots more message handlers.
            }
        }

        private void ProcessCarUpdate(KeyValuePair<ushort, CarPhysics> carUpdate)
        {
            var actorId = carUpdate.Key;
            var carPhysics = carUpdate.Value;
            var playerIndex = playerMapping.PlayerIndexFromActorId(actorId);
            if (playerIndex.HasValue)
            {
                var car = gameTickPacket.gameCars[playerIndex.Value];
                car.physics = carPhysics.physics;
                car.isSuperSonic = car.physics.velocity.Magnitude() >= 2200;

                switch (carPhysics.carState)
                {
                    case CarState.OnGround:
                        car.airState = rlbot.flat.AirState.OnGround;
                        car.dodgeTimeout = -1;
                        car.demolishedTimeout = -1;
                        car.lastJumpedFrame = gameTickPacket.frameNum;
                        break;
                    case CarState.Jumping:
                        car.airState = rlbot.flat.AirState.Jumping;
                        car.dodgeTimeout = 1.25f;
                        car.demolishedTimeout = -1;
                        car.lastJumpedFrame = gameTickPacket.frameNum;
                        break;
                    case CarState.DoubleJumping:
                        car.airState = rlbot.flat.AirState.DoubleJumping;
                        car.dodgeTimeout = -1;
                        car.demolishedTimeout = -1;
                        break;
                    case CarState.Dodging:
                        car.airState = rlbot.flat.AirState.Dodging;
                        car.dodgeTimeout = -1;
                        car.demolishedTimeout = -1;
                        break;
                    case CarState.InAir:
                        car.airState = rlbot.flat.AirState.InAir;
                        car.demolishedTimeout = -1;

                        var a_frame_diff = gameTickPacket.frameNum - car.lastJumpedFrame;
                        var a_time_left = 1.25f - a_frame_diff / 120f;
                        car.dodgeTimeout = a_time_left < 0 ? -1 : a_time_left;
                        break;
                    case CarState.Demolished:
                        car.dodgeTimeout = -1;

                        var d_frame_diff = gameTickPacket.frameNum - car.firstDemolishedFrame;
                        var d_time = d_frame_diff / 120f;
                        // TODO: Support respawn time mutator
                        if (d_time > 3)
                        {
                            car.firstDemolishedFrame = gameTickPacket.frameNum;
                            d_time = 3;
                        }

                        car.demolishedTimeout = 3 - d_time;
                        break;
                }
            }
        }


        public bool NotMatchEnded()
        {
            return gameTickPacket.gameState != GameStateType.Ended;
        }
    }
}
