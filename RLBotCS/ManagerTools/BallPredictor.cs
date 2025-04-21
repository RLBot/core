using System.Runtime.InteropServices;
using rlbot.flat;
using RLBotCS.Model;

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

    [LibraryImport("rl_ball_sym", EntryPoint = "get_heatseeker_target_y")]
    private static partial float GetHeatseekerTargetY();

    [LibraryImport("rl_ball_sym", EntryPoint = "reset_heatseeker_target")]
    private static partial void ResetHeatseekerTarget();

    [LibraryImport("rl_ball_sym", EntryPoint = "set_heatseeker_target")]
    private static partial void SetHeatseekerTarget(byte blueGoal);

    [LibraryImport("rl_ball_sym", EntryPoint = "step")]
    private static unsafe partial BallSlice* Step(BallSlice ball, ushort ticks);

    [LibraryImport("rl_ball_sym", EntryPoint = "free_ball_slices")]
    private static unsafe partial void FreeBallSlices(BallSlice* slices, ushort ticks);

    private static Vec3 ToVec3(Vector3T vec) => new(vec.X, vec.Y, vec.Z);

    private static Vector3T ToVector3T(Vec3 vec) =>
        new()
        {
            X = vec.X,
            Y = vec.Y,
            Z = vec.Z,
        };

    public static PredictionMode CurrentMode { get; private set; } = PredictionMode.Standard;

    public static void SetMode(PredictionMode mode)
    {
        CurrentMode = mode;

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

    public static PredictionMode GetModeOf(MatchConfigurationT matchConfig)
    {
        if (matchConfig.GameMapUpk.Contains("Throwback"))
            return PredictionMode.StandardThrowback;

        return matchConfig.GameMode switch
        {
            GameMode.Dropshot => PredictionMode.Dropshot,
            GameMode.Hoops => PredictionMode.Hoops,
            GameMode.Heatseeker => PredictionMode.Heatseeker,
            _ => PredictionMode.Standard,
        };
    }

    public static PredictionMode UpdateMode(MatchConfigurationT matchConfig)
    {
        var mode = GetModeOf(matchConfig);
        SetMode(mode);
        return mode;
    }

    public static BallPredictionT Generate(
        float currentTime,
        BallInfoT currentBall,
        (TouchT, uint)? lastTouch
    )
    {
        BallSlice ball = new()
        {
            Time = currentTime,
            Location = ToVec3(currentBall.Physics.Location),
            LinearVelocity = ToVec3(currentBall.Physics.Velocity),
            AngularVelocity = ToVec3(currentBall.Physics.AngularVelocity),
        };

        const ushort numSeconds = 6;
        const ushort numSlices = numSeconds * 120;

        BallPredictionT ballPrediction = new()
        {
            Slices = new List<PredictionSliceT>(numSlices),
        };

        if (CurrentMode == PredictionMode.Heatseeker)
        {
            if (lastTouch is (TouchT latestTouch, uint touchingTeam))
            {
                if (currentTime - latestTouch.GameSeconds < 0.1)
                {
                    // Target goal is the opposite of the last touch
                    SetHeatseekerTarget(
                        touchingTeam == Team.Blue ? (byte)Team.Orange : (byte)Team.Blue
                    );
                }
                else if (GetHeatseekerTargetY() == 0 || MathF.Abs(ball.Location.Y) >= 4820)
                {
                    // We're very likely to hit a wall that will redirect the ball towards the other goal
                    SetHeatseekerTarget(ball.LinearVelocity.Y < 0 ? (byte)1 : (byte)0);
                }
            }
            else
            {
                // A goal happened, we're in kickoff
                ResetHeatseekerTarget();
            }
        }

        unsafe
        {
            var ballSlices = Step(ball, numSlices);
            if (ballSlices == null)
                return ballPrediction;

            for (int i = 0; i < numSlices; i++)
            {
                BallSlice rawSlice = ballSlices[i];

                PredictionSliceT slice = new()
                {
                    GameSeconds = rawSlice.Time,
                    Physics = new PhysicsT
                    {
                        Location = ToVector3T(rawSlice.Location),
                        Velocity = ToVector3T(rawSlice.LinearVelocity),
                        AngularVelocity = ToVector3T(rawSlice.AngularVelocity),
                    },
                };

                ballPrediction.Slices.Add(slice);
            }

            FreeBallSlices(ballSlices, numSlices);
        }

        return ballPrediction;
    }
}
