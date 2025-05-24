using Bridge.Models.Message;
using Bridge.Packet;
using Bridge.State;
using RLBot.Flat;
using RLBotCS.Model;
using CollisionShapeUnion = RLBot.Flat.CollisionShapeUnion;
using MatchPhase = Bridge.Models.Message.MatchPhase;
using Rotator = Bridge.Models.Phys.Rotator;
using Vector2 = Bridge.Models.Phys.Vector2;
using Vector3 = Bridge.Models.Phys.Vector3;

namespace RLBotCS.Conversion;

static class GameStateToFlat
{
    private static Vector2T ToVector2T(this Vector2 vec) => new() { X = vec.X, Y = vec.Y };

    private static Vector3T ToVector3T(this Vector3 vec) =>
        new()
        {
            X = vec.X,
            Y = vec.Y,
            Z = vec.Z,
        };

    private static RotatorT ToRotatorT(this Rotator vec) =>
        new()
        {
            Pitch = vec.Pitch,
            Yaw = vec.Yaw,
            Roll = vec.Roll,
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

    public static GamePacketT ToFlatBuffers(this GameState gameState)
    {
        List<BallInfoT> balls = new(gameState.Balls.Count);
        foreach (var ball in gameState.Balls.Values)
        {
            // Create the ball info
            PhysicsT ballPhysics = new()
            {
                Location = ball.Physics.Location.ToVector3T(),
                Rotation = ball.Physics.Rotation.ToRotatorT(),
                Velocity = ball.Physics.Velocity.ToVector3T(),
                AngularVelocity = ball.Physics.AngularVelocity.ToVector3T(),
            };

            CollisionShapeUnion collisionShape = ball.Shape switch
            {
                ICollisionShape.Box boxShape => CollisionShapeUnion.FromBoxShape(
                    new()
                    {
                        Length = boxShape.Length,
                        Width = boxShape.Width,
                        Height = boxShape.Height,
                    }
                ),
                ICollisionShape.Sphere sphereShape => CollisionShapeUnion.FromSphereShape(
                    new() { Diameter = sphereShape.Diameter }
                ),
                ICollisionShape.Cylinder cylinderShape =>
                    CollisionShapeUnion.FromCylinderShape(
                        new()
                        {
                            Diameter = cylinderShape.Diameter,
                            Height = cylinderShape.Height,
                        }
                    ),
                _ => CollisionShapeUnion.FromSphereShape(
                    new SphereShapeT { Diameter = 91.25f * 2 }
                ),
            };

            balls.Add(new() { Physics = ballPhysics, Shape = collisionShape });
        }

        RLBot.Flat.MatchPhase matchPhase = gameState.MatchPhase switch
        {
            MatchPhase.Inactive => RLBot.Flat.MatchPhase.Inactive,
            MatchPhase.Countdown => RLBot.Flat.MatchPhase.Countdown,
            MatchPhase.Kickoff => RLBot.Flat.MatchPhase.Kickoff,
            MatchPhase.Active => RLBot.Flat.MatchPhase.Active,
            MatchPhase.GoalScored => RLBot.Flat.MatchPhase.GoalScored,
            MatchPhase.Replay => RLBot.Flat.MatchPhase.Replay,
            MatchPhase.Paused => RLBot.Flat.MatchPhase.Paused,
            MatchPhase.Ended => RLBot.Flat.MatchPhase.Ended,
            _ => RLBot.Flat.MatchPhase.Inactive,
        };

        MatchInfoT matchInfo = new()
        {
            SecondsElapsed = gameState.SecondsElapsed,
            GameTimeRemaining = gameState.GameTimeRemaining,
            IsOvertime = gameState.IsOvertime,
            IsUnlimitedTime = gameState.MatchLength == MatchLength.Unlimited,
            MatchPhase = matchPhase,
            WorldGravityZ = gameState.WorldGravityZ,
            GameSpeed = gameState.GameSpeed,
            LastSpectated = gameState.LastSpectated,
            FrameNum = gameState.FrameNum,
        };

        List<TeamInfoT> teams =
        [
            new() { TeamIndex = Team.Blue, Score = gameState.TeamScores.blue },
            new() { TeamIndex = Team.Orange, Score = gameState.TeamScores.orange },
        ];

        List<BoostPadStateT> boostStates = gameState
            .BoostPads.Values.OrderBy(boost => boost.SpawnPosition.Y)
            .ThenBy(boost => boost.SpawnPosition.X)
            .Select(boost => new BoostPadStateT
            {
                IsActive = boost.IsActive,
                Timer = boost.Timer,
            })
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
                _ => AirState.OnGround,
            };

            TouchT? lastTouch = null;
            if (car.LastTouch is BallTouch touch)
                lastTouch = new()
                {
                    GameSeconds = touch.TimeSeconds,
                    Location = touch.HitLocation.ToVector3T(),
                    Normal = touch.HitNormal.ToVector3T(),
                    BallIndex = touch.BallIndex,
                };

            players.Add(
                new()
                {
                    Physics = new()
                    {
                        Location = car.Physics.Location.ToVector3T(),
                        Rotation = car.Physics.Rotation.ToRotatorT(),
                        Velocity = car.Physics.Velocity.ToVector3T(),
                        AngularVelocity = car.Physics.AngularVelocity.ToVector3T(),
                    },
                    LatestTouch = lastTouch,
                    AirState = airState,
                    DodgeTimeout = car.DodgeTimeout,
                    DemolishedTimeout = car.DemolishedTimeout,
                    IsSupersonic = car.IsSuperSonic,
                    IsBot = car.IsCustomBot,
                    Name = car.Name,
                    Team = car.Team,
                    Boost = car.Boost,
                    SpawnId = car.SpawnId,
                    ScoreInfo = new()
                    {
                        Score = car.ScoreInfo.Score,
                        Goals = car.ScoreInfo.Goals,
                        OwnGoals = car.ScoreInfo.OwnGoals,
                        Assists = car.ScoreInfo.Assists,
                        Saves = car.ScoreInfo.Saves,
                        Shots = car.ScoreInfo.Shots,
                        Demolitions = car.ScoreInfo.Demolitions,
                    },
                    Hitbox = new()
                    {
                        Length = car.Hitbox.Length,
                        Width = car.Hitbox.Width,
                        Height = car.Hitbox.Height,
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
                        Handbrake = car.LastInput.Handbrake,
                    },
                    HasJumped = car.HasJumped,
                    HasDoubleJumped = car.HasDoubleJumped,
                    HasDodged = car.HasDodged,
                    DodgeElapsed = car.DodgeElapsed,
                    DodgeDir = car.DodgeDir.ToVector2T(),
                }
            );
        }

        return new GamePacketT
        {
            Balls = balls,
            MatchInfo = matchInfo,
            Teams = teams,
            BoostPads = boostStates,
            Players = players,
        };
    }
}
