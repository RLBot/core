using Google.FlatBuffers;
using RLBotCS.Server;

namespace RLBotCS.RLBotPacket
{
    internal class GameTickPacket
    {
        public SortedDictionary<int, GameCar> gameCars = new();
        public List<BoostPadStatus> gameBoosts = new();
        public Ball ball = new();


        // TODO: add gameInfo and teams fields.

        internal TypedPayload ToFlatbuffer()
        {
            FlatBufferBuilder builder = new(10000);

            // create the ball info

            rlbot.flat.Physics.StartPhysics(builder);

            var ball_location = rlbot.flat.Vector3.CreateVector3(builder, ball.physics.location.x, ball.physics.location.y, ball.physics.location.z);
            rlbot.flat.Physics.AddLocation(builder, ball_location);

            var ball_velocity = rlbot.flat.Vector3.CreateVector3(builder, ball.physics.velocity.x, ball.physics.velocity.y, ball.physics.velocity.z);
            rlbot.flat.Physics.AddVelocity(builder, ball_velocity);

            var ball_angular_velocity = rlbot.flat.Vector3.CreateVector3(builder, ball.physics.angularVelocity.x, ball.physics.angularVelocity.y, ball.physics.angularVelocity.z);
            rlbot.flat.Physics.AddAngularVelocity(builder, ball_angular_velocity);

            var ball_rotation = rlbot.flat.Rotator.CreateRotator(builder, ball.physics.rotation.pitch, ball.physics.rotation.yaw, ball.physics.rotation.roll);
            rlbot.flat.Physics.AddRotation(builder, ball_rotation);

            var ball_physics = rlbot.flat.Physics.EndPhysics(builder);

            rlbot.flat.Touch.StartTouch(builder);

            var last_touch_player_name = builder.CreateString(ball.latestTouch.playerName);
            rlbot.flat.Touch.AddPlayerName(builder, last_touch_player_name);

            rlbot.flat.Touch.AddPlayerIndex(builder, ball.latestTouch.playerIndex);
            rlbot.flat.Touch.AddTeam(builder, ball.latestTouch.team);
            rlbot.flat.Touch.AddGameSeconds(builder, ball.latestTouch.timeSeconds);

            var last_touch_location = rlbot.flat.Vector3.CreateVector3(builder, ball.latestTouch.hitLocation.x, ball.latestTouch.hitLocation.y, ball.latestTouch.hitLocation.z);
            rlbot.flat.Touch.AddLocation(builder, last_touch_location);

            var last_touch_normal = rlbot.flat.Vector3.CreateVector3(builder, ball.latestTouch.hitNormal.x, ball.latestTouch.hitNormal.y, ball.latestTouch.hitNormal.z);
            rlbot.flat.Touch.AddNormal(builder, last_touch_normal);

            var last_touch = rlbot.flat.Touch.EndTouch(builder);

            var ballInfo = rlbot.flat.BallInfo.CreateBallInfo(
                builder,
                ball_physics,
                last_touch
            );

            rlbot.flat.GameTickPacket.StartGameTickPacket(builder);

            // TODO: add all the data
            rlbot.flat.GameTickPacket.AddBall(builder, ballInfo);

            // finish
            var gtp = rlbot.flat.GameTickPacket.EndGameTickPacket(builder);
            builder.Finish(gtp.Value);

            return TypedPayload.FromFlatBufferBuilder(DataType.GameTickPacket, builder);
        }
    }
}