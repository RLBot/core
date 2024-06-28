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

    public static TypedPayload ToFlatBuffers(this GameState gameState, FlatBufferBuilder builder)
    {
        // Create the ball info
        PhysicsT ballPhysics =
            new()
            {
                Location = gameState.Ball.Physics.Location.ToVector3T(),
                Rotation = gameState.Ball.Physics.Rotation.ToRotatorT(),
                Velocity = gameState.Ball.Physics.Velocity.ToVector3T(),
                AngularVelocity = gameState.Ball.Physics.AngularVelocity.ToVector3T()
            };

        TouchT lastTouch =
            new()
            {
                PlayerName = gameState.Ball.LatestTouch.PlayerName,
                PlayerIndex = gameState.Ball.LatestTouch.PlayerIndex,
                Team = gameState.Ball.LatestTouch.Team,
                GameSeconds = gameState.Ball.LatestTouch.TimeSeconds,
                Location = gameState.Ball.LatestTouch.HitLocation.ToVector3T(),
                Normal = gameState.Ball.LatestTouch.HitNormal.ToVector3T()
            };

        CollisionShapeUnion collisionShape = gameState.Ball.Shape switch
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
                => CollisionShapeUnion.FromSphereShape(new() { Diameter = sphereShape.Diameter }),
            ICollisionShape.Cylinder cylinderShape
                => CollisionShapeUnion.FromCylinderShape(
                    new() { Diameter = cylinderShape.Diameter, Height = cylinderShape.Height }
                ),
            _ => CollisionShapeUnion.FromSphereShape(new SphereShapeT { Diameter = 91.25f * 2 })
        };

        BallInfoT ballInfo =
            new()
            {
                Physics = ballPhysics,
                LatestTouch = lastTouch,
                Shape = collisionShape
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
            .GameBoosts.Select(boost => new BoostPadStateT { IsActive = boost.IsActive, Timer = boost.Timer })
            .ToList();

        List<PlayerInfoT> players = [];
        for (uint i = 0; i < (uint)gameState.GameCars.Count; i++)
        {
            if (!gameState.GameCars.ContainsKey(i))
                // Often, at the start of a match,
                // not all the car data will be present.
                // Just skip appending players for now.
                break;

            var airState = gameState.GameCars[i].CarState switch
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
                        Location = gameState.GameCars[i].Physics.Location.ToVector3T(),
                        Rotation = gameState.GameCars[i].Physics.Rotation.ToRotatorT(),
                        Velocity = gameState.GameCars[i].Physics.Velocity.ToVector3T(),
                        AngularVelocity = gameState.GameCars[i].Physics.AngularVelocity.ToVector3T()
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
                        Demolitions = gameState.GameCars[i].ScoreInfo.Demolitions
                    },
                    Hitbox = new()
                    {
                        Length = gameState.GameCars[i].Hitbox.Length,
                        Width = gameState.GameCars[i].Hitbox.Width,
                        Height = gameState.GameCars[i].Hitbox.Height
                    },
                    HitboxOffset = gameState.GameCars[i].HitboxOffset.ToVector3T()
                }
            );
        }

        var gameTickPacket = new GameTickPacketT
        {
            Ball = ballInfo,
            GameInfo = gameInfo,
            Teams = teams,
            BoostPadStates = boostStates,
            Players = players
        };

        builder.Clear();
        builder.Finish(GameTickPacket.Pack(builder, gameTickPacket).Value);

        return TypedPayload.FromFlatBufferBuilder(DataType.GameTickPacket, builder);
    }
}
