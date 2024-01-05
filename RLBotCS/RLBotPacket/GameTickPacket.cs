using Google.FlatBuffers;
using RLBotCS.Server;

namespace RLBotCS.RLBotPacket
{
    internal class GameTickPacket
    {
        public SortedDictionary<int, GameCar> gameCars = new();
        public List<BoostPadStatus> gameBoosts = new();
        public Ball ball = new();
        public bool isOvertime = false;
        public bool isRoundActive = false;
        public bool isKickoffPause = false;
        public bool isMatchEnded = false;
        public float worldGravityZ = -650;

        // TODO: add gameInfo and teams fields.

        internal TypedPayload ToFlatbuffer()
        {
            // Create the ball info
            rlbot.flat.PhysicsT ballPhysics = new() {
                Location = new() {
                    X = ball.physics.location.x,
                    Y = ball.physics.location.y,
                    Z = ball.physics.location.z
                },
                Rotation = new() {
                    Pitch = ball.physics.rotation.pitch,
                    Yaw = ball.physics.rotation.yaw,
                    Roll = ball.physics.rotation.roll
                },
                Velocity = new() {
                    X = ball.physics.velocity.x,
                    Y = ball.physics.velocity.y,
                    Z = ball.physics.velocity.z
                },
                AngularVelocity = new() {
                    X = ball.physics.angularVelocity.x,
                    Y = ball.physics.angularVelocity.y,
                    Z = ball.physics.angularVelocity.z
                },
            };

            rlbot.flat.TouchT lastTouch = new() {
                PlayerName = ball.latestTouch.playerName,
                PlayerIndex = ball.latestTouch.playerIndex,
                Team = ball.latestTouch.team,
                GameSeconds = ball.latestTouch.timeSeconds,
                Location = new() {
                    X = ball.latestTouch.hitLocation.x,
                    Y = ball.latestTouch.hitLocation.y,
                    Z = ball.latestTouch.hitLocation.z
                },
                Normal = new() {
                    X = ball.latestTouch.hitNormal.x,
                    Y = ball.latestTouch.hitNormal.y,
                    Z = ball.latestTouch.hitNormal.z
                }
            };

            rlbot.flat.BallInfoT ballInfo = new() {
                Physics = ballPhysics,
                LatestTouch = lastTouch
            };

            // TODO: SecondsElapsed, GameTimeRemaining, IsUnlimitedTime, GameSpeed, and FrameNum
            rlbot.flat.GameInfoT gameInfo = new() {
                SecondsElapsed = 0,
                GameTimeRemaining = 0,
                IsOvertime = isOvertime,
                IsUnlimitedTime = false,
                IsRoundActive = isRoundActive,
                IsKickoffPause = isKickoffPause,
                IsMatchEnded = isMatchEnded,
                WorldGravityZ = worldGravityZ,
                GameSpeed = 1,
                FrameNum = 0,
            };

            List<rlbot.flat.TeamInfoT> teams = [
                new() {
                    TeamIndex = 0,
                    Score = 0,
                },
                new() {
                    TeamIndex = 1,
                    Score = 0,
                },
            ];

            // TODO: add BoostPadStates, Players, and maybe even TileInformation
            var gameTickPacket = new rlbot.flat.GameTickPacketT() {
                Ball = ballInfo,
                GameInfo = gameInfo,
                Teams = teams,
            };

            // A game tick packet is a bit over 8kb
            FlatBufferBuilder builder = new(8500);
            builder.Finish(rlbot.flat.GameTickPacket.Pack(builder, gameTickPacket).Value);

            return TypedPayload.FromFlatBufferBuilder(DataType.GameTickPacket, builder);
        }
    }
}