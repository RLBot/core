using Bridge.Models.Message;
using Bridge.State;
using Google.FlatBuffers;
using rlbot.flat;
using RLBotCS.Types;
using CollisionShapeUnion = rlbot.flat.CollisionShapeUnion;
using GameStateType = Bridge.Models.Message.GameStateType;

namespace RLBotCS.Conversion;

internal static class GameStateToFlat
{
    private static Vector3T ToVector3T(this Bridge.Models.Phys.Vector3 vec) =>
        new()
        {
            X = vec.X,
            Y = vec.Y,
            Z = vec.Z
        };

    private static RotatorT ToRotatorT(this Bridge.Models.Phys.Rotator vec) =>
        new()
        {
            Pitch = vec.Pitch,
            Yaw = vec.Yaw,
            Roll = vec.Roll
        };

    internal static ushort? GetBallActorIdFromIndex(this GameState gameState, uint index)
    {
        try
        {
            return gameState.Balls.ElementAt((int)index).Key;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    public static GameTickPacketT ToFlatBuffers(this GameState gameState)
    {
        List<BallInfoT> balls = new(gameState.Balls.Count);
        foreach (var ball in gameState.Balls.Values)
        {
            // Create the ball info
            PhysicsT ballPhysics =
                new()
                {
                    Location = ball.Physics.Location.ToVector3T(),
                    Rotation = ball.Physics.Rotation.ToRotatorT(),
                    Velocity = ball.Physics.Velocity.ToVector3T(),
                    AngularVelocity = ball.Physics.AngularVelocity.ToVector3T()
                };

            TouchT lastTouch =
                new()
                {
                    PlayerName = ball.LatestTouch.PlayerName,
                    PlayerIndex = ball.LatestTouch.PlayerIndex,
                    Team = ball.LatestTouch.Team,
                    GameSeconds = ball.LatestTouch.TimeSeconds,
                    Location = ball.LatestTouch.HitLocation.ToVector3T(),
                    Normal = ball.LatestTouch.HitNormal.ToVector3T()
                };

            CollisionShapeUnion collisionShape = ball.Shape switch
            {
                ICollisionShape.Box boxShape
                    => CollisionShapeUnion.FromBoxShape(
                        new()
                        {
                            Length = boxShape.Length,
                            Width = boxShape.Width,
                            Height = boxShape.Height
                        }
                    ),
                ICollisionShape.Sphere sphereShape
                    => CollisionShapeUnion.FromSphereShape(
                        new() { Diameter = sphereShape.Diameter }
                    ),
                ICollisionShape.Cylinder cylinderShape
                    => CollisionShapeUnion.FromCylinderShape(
                        new()
                        {
                            Diameter = cylinderShape.Diameter,
                            Height = cylinderShape.Height
                        }
                    ),
                _
                    => CollisionShapeUnion.FromSphereShape(
                        new SphereShapeT { Diameter = 91.25f * 2 }
                    )
            };

            balls.Add(
                new()
                {
                    Physics = ballPhysics,
                    LatestTouch = lastTouch,
                    Shape = collisionShape
                }
            );
        }

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
            _ => rlbot.flat.GameStateType.Inactive
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
                FrameNum = gameState.FrameNum
            };

        List<TeamInfoT> teams =
        [
            new() { TeamIndex = 0, Score = gameState.TeamScores.blue },
            new() { TeamIndex = 1, Score = gameState.TeamScores.orange }
        ];

        List<BoostPadStateT> boostStates = gameState
            .BoostPads.Values.Select(
                boost => new BoostPadStateT { IsActive = boost.IsActive, Timer = boost.Timer }
            )
            .ToList();

        List<PlayerInfoT> players = new(gameState.GameCars.Count);
        foreach (var car in gameState.GameCars.Values)
        {
            var airState = car.CarState switch
            {
                CarState.OnGround => AirState.OnGround,
                CarState.Jumping => AirState.Jumping,
                CarState.DoubleJumping => AirState.DoubleJumping,
                CarState.Dodging => AirState.Dodging,
                CarState.InAir => AirState.InAir,
                _ => AirState.OnGround
            };

            players.Add(
                new()
                {
                    Physics = new()
                    {
                        Location = car.Physics.Location.ToVector3T(),
                        Rotation = car.Physics.Rotation.ToRotatorT(),
                        Velocity = car.Physics.Velocity.ToVector3T(),
                        AngularVelocity = car.Physics.AngularVelocity.ToVector3T()
                    },
                    AirState = airState,
                    DodgeTimeout = car.DodgeTimeout,
                    DemolishedTimeout = car.DemolishedTimeout,
                    IsSupersonic = car.IsSuperSonic,
                    IsBot = car.IsCustomBot,
                    Name = car.Name,
                    Team = car.Team,
                    Boost = (uint)Math.Floor(car.Boost),
                    SpawnId = car.SpawnId,
                    ScoreInfo = new()
                    {
                        Score = car.ScoreInfo.Score,
                        Goals = car.ScoreInfo.Goals,
                        OwnGoals = car.ScoreInfo.OwnGoals,
                        Assists = car.ScoreInfo.Assists,
                        Saves = car.ScoreInfo.Saves,
                        Shots = car.ScoreInfo.Shots,
                        Demolitions = car.ScoreInfo.Demolitions
                    },
                    Hitbox = new()
                    {
                        Length = car.Hitbox.Length,
                        Width = car.Hitbox.Width,
                        Height = car.Hitbox.Height
                    },
                    HitboxOffset = car.HitboxOffset.ToVector3T(),
                    Accolades = car.Accolades,
                    LastInput = new()
                    {
                        Throttle = car.LastInput.Throttle,
                        Steer = car.LastInput.Steer,
                        Pitch = car.LastInput.Pitch,
                        Yaw = car.LastInput.Yaw,
                        Roll = car.LastInput.Roll,
                        Jump = car.LastInput.Jump,
                        Boost = car.LastInput.Boost,
                        Handbrake = car.LastInput.Handbrake
                    },
                    LastSpectated = car.LastSpectated,
                }
            );
        }

        return new GameTickPacketT
        {
            Balls = balls,
            GameInfo = gameInfo,
            Teams = teams,
            BoostPadStates = boostStates,
            Players = players
        };
    }
}
