using Google.FlatBuffers;
using RLBotCS.Server;
using RLBotModels.Message;

namespace RLBotCS.RLBotPacket
{
    internal class GameTickPacket
    {
        public SortedDictionary<int, GameCar> gameCars = new();
        public List<BoostPadStatus> gameBoosts = new();
        public Ball ball = new();
        public bool isOvertime = false;
        public bool isUnlimitedTime = false;
        public GameStateType gameState = GameStateType.Inactive;
        public float worldGravityZ = -650;
        public float secondsElapsed = 0;
        public float gameTimeRemaining = 0;
        public float gameSpeed = 1;
        public int frameNum = 0;
        public List<int> teamScores = [0, 0];

        // TODO: tile information?

        internal TypedPayload ToFlatbuffer()
        {
            // Create the ball info
            rlbot.flat.PhysicsT ballPhysics =
                new()
                {
                    Location = new()
                    {
                        X = ball.physics.location.x,
                        Y = ball.physics.location.y,
                        Z = ball.physics.location.z
                    },
                    Rotation = new()
                    {
                        Pitch = ball.physics.rotation.pitch,
                        Yaw = ball.physics.rotation.yaw,
                        Roll = ball.physics.rotation.roll
                    },
                    Velocity = new()
                    {
                        X = ball.physics.velocity.x,
                        Y = ball.physics.velocity.y,
                        Z = ball.physics.velocity.z
                    },
                    AngularVelocity = new()
                    {
                        X = ball.physics.angularVelocity.x,
                        Y = ball.physics.angularVelocity.y,
                        Z = ball.physics.angularVelocity.z
                    },
                };

            rlbot.flat.TouchT lastTouch =
                new()
                {
                    PlayerName = ball.latestTouch.playerName,
                    PlayerIndex = ball.latestTouch.playerIndex,
                    Team = ball.latestTouch.team,
                    GameSeconds = ball.latestTouch.timeSeconds,
                    Location = new()
                    {
                        X = ball.latestTouch.hitLocation.x,
                        Y = ball.latestTouch.hitLocation.y,
                        Z = ball.latestTouch.hitLocation.z
                    },
                    Normal = new()
                    {
                        X = ball.latestTouch.hitNormal.x,
                        Y = ball.latestTouch.hitNormal.y,
                        Z = ball.latestTouch.hitNormal.z
                    }
                };

            rlbot.flat.CollisionShapeUnion collisionShape = ball.shape.Type switch
            {
                CollisionShape.BoxShape
                    => rlbot.flat.CollisionShapeUnion.FromBoxShape(
                        new()
                        {
                            Length = ball.shape.As<BoxShape>().Length,
                            Width = ball.shape.As<BoxShape>().Width,
                            Height = ball.shape.As<BoxShape>().Height,
                        }
                    ),
                CollisionShape.SphereShape
                    => rlbot.flat.CollisionShapeUnion.FromSphereShape(
                        new() { Diameter = ball.shape.As<SphereShape>().Diameter, }
                    ),
                CollisionShape.CylinderShape
                    => rlbot.flat.CollisionShapeUnion.FromCylinderShape(
                        new()
                        {
                            Diameter = ball.shape.As<CylinderShape>().Diameter,
                            Height = ball.shape.As<CylinderShape>().Height,
                        }
                    ),
                _ => rlbot.flat.CollisionShapeUnion.FromSphereShape(new() { Diameter = 91.25f * 2, }),
            };

            rlbot.flat.BallInfoT ballInfo =
                new()
                {
                    Physics = ballPhysics,
                    LatestTouch = lastTouch,
                    Shape = collisionShape,
                };

            rlbot.flat.GameStateType gameStateType = gameState switch
            {
                GameStateType.Inactive => rlbot.flat.GameStateType.Inactive,
                GameStateType.Countdown => rlbot.flat.GameStateType.Countdown,
                GameStateType.Kickoff => rlbot.flat.GameStateType.Kickoff,
                GameStateType.Active => rlbot.flat.GameStateType.Active,
                GameStateType.GoalScored => rlbot.flat.GameStateType.GoalScored,
                GameStateType.Replay => rlbot.flat.GameStateType.Replay,
                GameStateType.Paused => rlbot.flat.GameStateType.Paused,
                GameStateType.Ended => rlbot.flat.GameStateType.Ended,
                _ => rlbot.flat.GameStateType.Inactive,
            };

            rlbot.flat.GameInfoT gameInfo =
                new()
                {
                    SecondsElapsed = secondsElapsed,
                    GameTimeRemaining = gameTimeRemaining,
                    IsOvertime = isOvertime,
                    IsUnlimitedTime = isUnlimitedTime,
                    GameStateType = gameStateType,
                    WorldGravityZ = worldGravityZ,
                    GameSpeed = gameSpeed,
                    FrameNum = frameNum,
                };

            List<rlbot.flat.TeamInfoT> teams = new();
            for (var i = 0; i < teamScores.Count; i++)
            {
                teams.Add(new() { TeamIndex = i, Score = teamScores[i], });
            }

            List<rlbot.flat.BoostPadStateT> boostStates = new();
            foreach (var boost in gameBoosts)
            {
                boostStates.Add(new() { IsActive = boost.isActive, Timer = boost.timer, });
            }

            List<rlbot.flat.PlayerInfoT> players = new();
            for (var i = 0; i < gameCars.Count; i++)
            {
                if (!gameCars.ContainsKey(i))
                {
                    players.Add(new());
                    continue;
                }

                players.Add(
                    new()
                    {
                        Physics = new()
                        {
                            Location = new()
                            {
                                X = gameCars[i].physics.location.x,
                                Y = gameCars[i].physics.location.y,
                                Z = gameCars[i].physics.location.z,
                            },
                            Rotation = new()
                            {
                                Pitch = gameCars[i].physics.rotation.pitch,
                                Yaw = gameCars[i].physics.rotation.yaw,
                                Roll = gameCars[i].physics.rotation.roll,
                            },
                            Velocity = new()
                            {
                                X = gameCars[i].physics.velocity.x,
                                Y = gameCars[i].physics.velocity.y,
                                Z = gameCars[i].physics.velocity.z,
                            },
                            AngularVelocity = new()
                            {
                                X = gameCars[i].physics.angularVelocity.x,
                                Y = gameCars[i].physics.angularVelocity.y,
                                Z = gameCars[i].physics.angularVelocity.z,
                            },
                        },
                        AirState = gameCars[i].airState,
                        DodgeTimeout = gameCars[i].dodgeTimeout,
                        DemolishedTimeout = gameCars[i].demolishedTimeout,
                        IsSupersonic = gameCars[i].isSuperSonic,
                        IsBot = gameCars[i].isBot,
                        Name = gameCars[i].name,
                        Team = gameCars[i].team,
                        Boost = (int)Math.Floor(gameCars[i].boost),
                        SpawnId = gameCars[i].spawnId,
                        ScoreInfo = new()
                        {
                            Score = gameCars[i].scoreInfo.score,
                            Goals = gameCars[i].scoreInfo.goals,
                            OwnGoals = gameCars[i].scoreInfo.ownGoals,
                            Assists = gameCars[i].scoreInfo.assists,
                            Saves = gameCars[i].scoreInfo.saves,
                            Shots = gameCars[i].scoreInfo.shots,
                            Demolitions = gameCars[i].scoreInfo.demolitions,
                        },
                        Hitbox = new()
                        {
                            Length = gameCars[i].hitbox.length,
                            Width = gameCars[i].hitbox.width,
                            Height = gameCars[i].hitbox.height,
                        },
                        HitboxOffset = new()
                        {
                            X = gameCars[i].hitboxOffset.x,
                            Y = gameCars[i].hitboxOffset.y,
                            Z = gameCars[i].hitboxOffset.z,
                        },
                    }
                );
            }

            var gameTickPacket = new rlbot.flat.GameTickPacketT()
            {
                Ball = ballInfo,
                GameInfo = gameInfo,
                Teams = teams,
                BoostPadStates = boostStates,
                Players = players,
            };

            // A game tick packet is a bit over 8kb
            FlatBufferBuilder builder = new(8500);
            builder.Finish(rlbot.flat.GameTickPacket.Pack(builder, gameTickPacket).Value);

            return TypedPayload.FromFlatBufferBuilder(DataType.GameTickPacket, builder);
        }
    }
}
