using Google.FlatBuffers;
using rlbot.flat;
using Bridge.State;
using Bridge.Types;
using BoxShape = Bridge.Models.Message.BoxShape;
using CollisionShape = Bridge.Models.Message.CollisionShape;
using CollisionShapeUnion = rlbot.flat.CollisionShapeUnion;
using CylinderShape = Bridge.Models.Message.CylinderShape;
using GameStateType = Bridge.Models.Message.GameStateType;
using SphereShape = Bridge.Models.Message.SphereShape;

namespace RLBotCS.Conversion
{
    internal static class GameStateToFlat
    {
        public static TypedPayload ToFlatbuffer(this GameState gameState, FlatBufferBuilder builder)
        {
            // Create the ball info
            PhysicsT ballPhysics =
                new()
                {
                    Location = new()
                    {
                        X = gameState.Ball.Physics.location.x,
                        Y = gameState.Ball.Physics.location.y,
                        Z = gameState.Ball.Physics.location.z
                    },
                    Rotation = new()
                    {
                        Pitch = gameState.Ball.Physics.rotation.pitch,
                        Yaw = gameState.Ball.Physics.rotation.yaw,
                        Roll = gameState.Ball.Physics.rotation.roll
                    },
                    Velocity = new()
                    {
                        X = gameState.Ball.Physics.velocity.x,
                        Y = gameState.Ball.Physics.velocity.y,
                        Z = gameState.Ball.Physics.velocity.z
                    },
                    AngularVelocity = new()
                    {
                        X = gameState.Ball.Physics.angularVelocity.x,
                        Y = gameState.Ball.Physics.angularVelocity.y,
                        Z = gameState.Ball.Physics.angularVelocity.z
                    },
                };

            TouchT lastTouch =
                new()
                {
                    PlayerName = gameState.Ball.LatestTouch.PlayerName,
                    PlayerIndex = gameState.Ball.LatestTouch.PlayerIndex,
                    Team = gameState.Ball.LatestTouch.Team,
                    GameSeconds = gameState.Ball.LatestTouch.TimeSeconds,
                    Location = new()
                    {
                        X = gameState.Ball.LatestTouch.HitLocation.x,
                        Y = gameState.Ball.LatestTouch.HitLocation.y,
                        Z = gameState.Ball.LatestTouch.HitLocation.z
                    },
                    Normal = new()
                    {
                        X = gameState.Ball.LatestTouch.HitNormal.x,
                        Y = gameState.Ball.LatestTouch.HitNormal.y,
                        Z = gameState.Ball.LatestTouch.HitNormal.z
                    }
                };

            CollisionShapeUnion collisionShape = gameState.Ball.Shape.Type switch
            {
                CollisionShape.BoxShape
                    => CollisionShapeUnion.FromBoxShape(
                        new()
                        {
                            Length = gameState.Ball.Shape.As<BoxShape>().Length,
                            Width = gameState.Ball.Shape.As<BoxShape>().Width,
                            Height = gameState.Ball.Shape.As<BoxShape>().Height,
                        }
                    ),
                CollisionShape.SphereShape
                    => CollisionShapeUnion.FromSphereShape(
                        new() { Diameter = gameState.Ball.Shape.As<SphereShape>().Diameter, }
                    ),
                CollisionShape.CylinderShape
                    => CollisionShapeUnion.FromCylinderShape(
                        new()
                        {
                            Diameter = gameState.Ball.Shape.As<CylinderShape>().Diameter,
                            Height = gameState.Ball.Shape.As<CylinderShape>().Height,
                        }
                    ),
                _ => CollisionShapeUnion.FromSphereShape(new() { Diameter = 91.25f * 2, }),
            };

            BallInfoT ballInfo =
                new()
                {
                    Physics = ballPhysics,
                    LatestTouch = lastTouch,
                    Shape = collisionShape,
                };

            rlbot.flat.GameStateType gameStateType = gameState.GameStateType switch
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

            GameInfoT gameInfo =
                new()
                {
                    SecondsElapsed = gameState.SecondsElapsed,
                    GameTimeRemaining = gameState.GameTimeRemaining,
                    IsOvertime = gameState.IsOvertime,
                    IsUnlimitedTime = gameState.MatchLength == Bridge.Packet.MatchLength.Unlimited,
                    GameStateType = gameStateType,
                    WorldGravityZ = gameState.WorldGravityZ,
                    GameSpeed = gameState.GameSpeed,
                    FrameNum = gameState.FrameNum,
                };

            List<TeamInfoT> teams =
                new()
                {
                    new() { TeamIndex = 0, Score = gameState.TeamScores.blue },
                    new() { TeamIndex = 1, Score = gameState.TeamScores.orange },
                };

            List<BoostPadStateT> boostStates = new();
            foreach (var boost in gameState.GameBoosts)
            {
                boostStates.Add(new() { IsActive = boost.IsActive, Timer = boost.Timer, });
            }

            List<PlayerInfoT> players = new();
            for (uint i = 0; i < (uint)gameState.GameCars.Count; i++)
            {
                if (!gameState.GameCars.ContainsKey(i))
                {
                    // Often, at the start of a match,
                    // not all the car data will be present.
                    // Just skip appending players for now.
                    break;
                }

                var airState = gameState.GameCars[i].AirState switch
                {
                    Bridge.Packet.AirState.OnGround => AirState.OnGround,
                    Bridge.Packet.AirState.Jumping => AirState.Jumping,
                    Bridge.Packet.AirState.DoubleJumping => AirState.DoubleJumping,
                    Bridge.Packet.AirState.Dodging => AirState.Dodging,
                    Bridge.Packet.AirState.InAir => AirState.InAir,
                    _ => AirState.OnGround,
                };

                players.Add(
                    new()
                    {
                        Physics = new()
                        {
                            Location = new()
                            {
                                X = gameState.GameCars[i].Physics.location.x,
                                Y = gameState.GameCars[i].Physics.location.y,
                                Z = gameState.GameCars[i].Physics.location.z,
                            },
                            Rotation = new()
                            {
                                Pitch = gameState.GameCars[i].Physics.rotation.pitch,
                                Yaw = gameState.GameCars[i].Physics.rotation.yaw,
                                Roll = gameState.GameCars[i].Physics.rotation.roll,
                            },
                            Velocity = new()
                            {
                                X = gameState.GameCars[i].Physics.velocity.x,
                                Y = gameState.GameCars[i].Physics.velocity.y,
                                Z = gameState.GameCars[i].Physics.velocity.z,
                            },
                            AngularVelocity = new()
                            {
                                X = gameState.GameCars[i].Physics.angularVelocity.x,
                                Y = gameState.GameCars[i].Physics.angularVelocity.y,
                                Z = gameState.GameCars[i].Physics.angularVelocity.z,
                            },
                        },
                        AirState = airState,
                        DodgeTimeout = gameState.GameCars[i].DodgeTimeout,
                        DemolishedTimeout = gameState.GameCars[i].DemolishedTimeout,
                        IsSupersonic = gameState.GameCars[i].IsSuperSonic,
                        IsBot = gameState.GameCars[i].IsBot,
                        Name = gameState.GameCars[i].Name,
                        Team = gameState.GameCars[i].Team,
                        Boost = (uint)Math.Floor(gameState.GameCars[i].Boost),
                        SpawnId = gameState.GameCars[i].SpawnId,
                        ScoreInfo = new()
                        {
                            Score = gameState.GameCars[i].ScoreInfo.Score,
                            Goals = gameState.GameCars[i].ScoreInfo.Goals,
                            OwnGoals = gameState.GameCars[i].ScoreInfo.OwnGoals,
                            Assists = gameState.GameCars[i].ScoreInfo.Assists,
                            Saves = gameState.GameCars[i].ScoreInfo.Saves,
                            Shots = gameState.GameCars[i].ScoreInfo.Shots,
                            Demolitions = gameState.GameCars[i].ScoreInfo.Demolitions,
                        },
                        Hitbox = new()
                        {
                            Length = gameState.GameCars[i].Hitbox.length,
                            Width = gameState.GameCars[i].Hitbox.width,
                            Height = gameState.GameCars[i].Hitbox.height,
                        },
                        HitboxOffset = new()
                        {
                            X = gameState.GameCars[i].HitboxOffset.x,
                            Y = gameState.GameCars[i].HitboxOffset.y,
                            Z = gameState.GameCars[i].HitboxOffset.z,
                        },
                    }
                );
            }

            var gameTickPacket = new GameTickPacketT
            {
                Ball = ballInfo,
                GameInfo = gameInfo,
                Teams = teams,
                BoostPadStates = boostStates,
                Players = players,
            };

            builder.Clear();
            builder.Finish(GameTickPacket.Pack(builder, gameTickPacket).Value);

            return TypedPayload.FromFlatBufferBuilder(DataType.GameTickPacket, builder);
        }
    }
}
