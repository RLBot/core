using System.Runtime.InteropServices;
using rlbot.flat;
using RLBotSecret.Packet;

namespace RLBotCS.ManagerTools;

[StructLayout(LayoutKind.Sequential)]
struct Vec3(float x, float y, float z)
{
    public float X = x;
    public float Y = y;
    public float Z = z;
}

[StructLayout(LayoutKind.Sequential)]
struct BallSlice
{
    public float Time;
    public Vec3 Location;
    public Vec3 LinearVelocity;
    public Vec3 AngularVelocity;
}

public enum PredictionMode
{
    Standard,
    Heatseeker,
    Dropshot,
    Hoops,
    StandardThrowback,
}

public static partial class BallPredictor
{
    [LibraryImport("rl_ball_sym", EntryPoint = "load_standard")]
    private static partial void LoadStandard();

    [LibraryImport("rl_ball_sym", EntryPoint = "load_heatseeker")]
    private static partial void LoadStandardHeatseeker();

    [LibraryImport("rl_ball_sym", EntryPoint = "load_dropshot")]
    private static partial void LoadDropshot();

    [LibraryImport("rl_ball_sym", EntryPoint = "load_hoops")]
    private static partial void LoadHoops();

    [LibraryImport("rl_ball_sym", EntryPoint = "load_standard_throwback")]
    private static partial void LoadStandardThrowback();

    [LibraryImport("rl_ball_sym", EntryPoint = "set_heatseeker_target")]
    private static partial void SetHeatseekerTarget(byte blueGoal);

    [LibraryImport("rl_ball_sym", EntryPoint = "step")]
    private static partial BallSlice Step(BallSlice ball);

    private static Vec3 ToVec3(RLBotSecret.Models.Phys.Vector3 vec) => new(vec.x, vec.y, vec.z);

    private static Vector3T ToVector3T(Vec3 vec) => new(vec.X, vec.Y, vec.Z);

    public static void SetMode(PredictionMode mode)
    {
        switch (mode)
        {
            case PredictionMode.Standard:
                LoadStandard();
                break;
            case PredictionMode.Heatseeker:
                LoadStandardHeatseeker();
                break;
            case PredictionMode.Dropshot:
                LoadDropshot();
                break;
            case PredictionMode.Hoops:
                LoadHoops();
                break;
            case PredictionMode.StandardThrowback:
                LoadStandardThrowback();
                break;
        }
    }

    public static PredictionMode GetMode(MatchSettingsT matchSettings)
    {
        if (matchSettings.GameMapUpk.Contains("Throwback"))
            return PredictionMode.StandardThrowback;

        return matchSettings.GameMode switch
        {
            GameMode.Dropshot => PredictionMode.Dropshot,
            GameMode.Hoops => PredictionMode.Hoops,
            GameMode.Heatseeker => PredictionMode.Heatseeker,
            _ => PredictionMode.Standard,
        };
    }

    public static BallPredictionT Generate(PredictionMode mode, float time, Ball currentBall)
    {
        BallSlice ball = new()
        {
            Time = time,
            Location = ToVec3(currentBall.Physics.location),
            LinearVelocity = ToVec3(currentBall.Physics.velocity),
            AngularVelocity = ToVec3(currentBall.Physics.angularVelocity)
        };

        const int numSeconds = 8;
        const int numSlices = numSeconds * 120;

        BallPredictionT ballPrediction = new(slices: new List<PredictionSliceT>(numSlices));

        if (mode == PredictionMode.Heatseeker)
            // Target goal is the opposite of the last touch
            SetHeatseekerTarget(currentBall.LatestTouch.Team == 0 ? (byte)1 : (byte)0);

        for (int i = 0; i < numSlices; i++)
        {
            ball = Step(ball);

            PredictionSliceT slice = new()
            {
                GameSeconds = ball.Time,
                Physics = new PhysicsT
                {
                    Location = ToVector3T(ball.Location),
                    Velocity = ToVector3T(ball.LinearVelocity),
                    AngularVelocity = ToVector3T(ball.AngularVelocity)
                }
            };

            ballPrediction.Slices.Add(slice);
        }

        return ballPrediction;
    }
}
