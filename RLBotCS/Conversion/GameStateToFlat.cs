using Bridge.State;
using Bridge.Types;
using Google.FlatBuffers;
using rlbot.flat;
using BoxShape = Bridge.Models.Message.BoxShape;
using CollisionShape = Bridge.Models.Message.CollisionShape;
using CollisionShapeUnion = rlbot.flat.CollisionShapeUnion;
using CylinderShape = Bridge.Models.Message.CylinderShape;
using GameStateType = Bridge.Models.Message.GameStateType;
using SphereShape = Bridge.Models.Message.SphereShape;

namespace RLBotCS.Conversion;

internal static class GameStateToFlat
{
    private static Vector3T ToVector3T(this Bridge.Models.Phys.Vector3 vec) =>
        new()
        {
            X = vec.x,
            Y = vec.y,
            Z = vec.z
        };

    private static RotatorT ToRotatorT(this Bridge.Models.Phys.Rotator vec) =>
        new()
        {
            Pitch = vec.pitch,
            Yaw = vec.yaw,
            Roll = vec.roll
        };

    public static TypedPayload ToFlatBuffers(this GameState gameState, FlatBufferBuilder builder)
    {
        // Create the ball info
        PhysicsT ballPhysics =
            new()
            {
                Location = gameState.Ball.Physics.location.ToVector3T(),
                Rotation = gameState.Ball.Physics.rotation.ToRotatorT(),
                Velocity = gameState.Ball.Physics.velocity.ToVector3T(),
                AngularVelocity = gameState.Ball.Physics.angularVelocity.ToVector3T()
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

        CollisionShapeUnion collisionShape = gameState.Ball.Shape.Type switch
        {
            CollisionShape.BoxShape
                => CollisionShapeUnion.FromBoxShape(
                    new()
                    {
                        Length = gameState.Ball.Shape.As<BoxShape>().Length,
                        Width = gameState.Ball.Shape.As<BoxShape>().Width,
                        Height = gameState.Ball.Shape.As<BoxShape>().Height
                    }
                ),
            CollisionShape.SphereShape
                => CollisionShapeUnion.FromSphereShape(
                    new() { Diameter = gameState.Ball.Shape.As<SphereShape>().Diameter }
                ),
            CollisionShape.CylinderShape
                => CollisionShapeUnion.FromCylinderShape(
                    new()
                    {
                        Diameter = gameState.Ball.Shape.As<CylinderShape>().Diameter,
                        Height = gameState.Ball.Shape.As<CylinderShape>().Height
                    }
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

            var airState = gameState.GameCars[i].AirState switch
            {
                Bridge.Packet.AirState.OnGround => AirState.OnGround,
                Bridge.Packet.AirState.Jumping => AirState.Jumping,
                Bridge.Packet.AirState.DoubleJumping => AirState.DoubleJumping,
                Bridge.Packet.AirState.Dodging => AirState.Dodging,
                Bridge.Packet.AirState.InAir => AirState.InAir,
                _ => AirState.OnGround
            };

            players.Add(
                new()
                {
                    Physics = new()
                    {
                        Location = gameState.GameCars[i].Physics.location.ToVector3T(),
                        Rotation = gameState.GameCars[i].Physics.rotation.ToRotatorT(),
                        Velocity = gameState.GameCars[i].Physics.velocity.ToVector3T(),
                        AngularVelocity = gameState.GameCars[i].Physics.angularVelocity.ToVector3T()
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
                        Length = gameState.GameCars[i].Hitbox.length,
                        Width = gameState.GameCars[i].Hitbox.width,
                        Height = gameState.GameCars[i].Hitbox.height
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
