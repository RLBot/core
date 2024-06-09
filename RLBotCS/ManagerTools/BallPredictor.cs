using System.Runtime.InteropServices;
using rlbot.flat;
using RLBotSecret.Packet;

namespace RLBotCS.MatchManagement
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vec3
    {
        public float x;
        public float y;
        public float z;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BallSlice
    {
        public float time;
        public Vec3 location;
        public Vec3 linear_velocity;
        public Vec3 angular_velocity;
    }

    public enum PredictionMode
    {
        STANDARD,
        DROPSHOT,
        HOOPS,
        STANDARD_THROWBACK,
    }

    public partial class BallPredictor
    {
        [LibraryImport("rl_ball_sym", EntryPoint = "load_standard")]
        private static partial void LoadStandard();

        [LibraryImport("rl_ball_sym", EntryPoint = "load_dropshot")]
        private static partial void LoadDropshot();

        [LibraryImport("rl_ball_sym", EntryPoint = "load_hoops")]
        private static partial void LoadHoops();

        [LibraryImport("rl_ball_sym", EntryPoint = "load_standard_throwback")]
        private static partial void LoadStandardThrowback();

        [LibraryImport("rl_ball_sym", EntryPoint = "step")]
        private static partial BallSlice Step(BallSlice ball);

        private static Vec3 ToVec3(RLBotSecret.Models.Phys.Vector3 vec)
        {
            return new Vec3
            {
                x = vec.x,
                y = vec.y,
                z = vec.z
            };
        }

        private static Vector3T ToVector3T(Vec3 vec)
        {
            return new Vector3T()
            {
                X = vec.x,
                Y = vec.y,
                Z = vec.z
            };
        }

        private PredictionMode? _mode = null;

        public BallPredictor(PredictionMode mode)
        {
            SetMode(mode);
        }

        private void SetMode(PredictionMode mode)
        {
            if (_mode == mode)
            {
                return;
            }

            _mode = mode;

            switch (_mode)
            {
                case PredictionMode.STANDARD:
                    LoadStandard();
                    break;
                case PredictionMode.DROPSHOT:
                    LoadDropshot();
                    break;
                case PredictionMode.HOOPS:
                    LoadHoops();
                    break;
                case PredictionMode.STANDARD_THROWBACK:
                    LoadStandardThrowback();
                    break;
            }
        }

        public void Sync(MatchSettingsT matchSettings)
        {
            PredictionMode mode = PredictionMode.STANDARD;

            switch (matchSettings.GameMode)
            {
                case GameMode.Dropshot:
                    mode = PredictionMode.DROPSHOT;
                    break;
                case GameMode.Hoops:
                    mode = PredictionMode.HOOPS;
                    break;
            }

            if (matchSettings.GameMapUpk.Contains("Throwback"))
            {
                mode = PredictionMode.STANDARD_THROWBACK;
            }

            SetMode(mode);
        }

        public BallPredictionT Generate(float time, Ball current_ball)
        {
            BallSlice ball = new BallSlice
            {
                time = time,
                location = ToVec3(current_ball.Physics.location),
                linear_velocity = ToVec3(current_ball.Physics.velocity),
                angular_velocity = ToVec3(current_ball.Physics.angularVelocity)
            };

            BallPredictionT ballPrediction = new BallPredictionT() { Slices = new List<PredictionSliceT>() };

            for (int i = 0; i < 8 * 120; i++)
            {
                ball = Step(ball);

                PredictionSliceT slice = new PredictionSliceT()
                {
                    GameSeconds = ball.time,
                    Physics = new PhysicsT()
                    {
                        Location = ToVector3T(ball.location),
                        Velocity = ToVector3T(ball.linear_velocity),
                        AngularVelocity = ToVector3T(ball.angular_velocity)
                    }
                };

                ballPrediction.Slices.Add(slice);
            }

            return ballPrediction;
        }
    }
}
