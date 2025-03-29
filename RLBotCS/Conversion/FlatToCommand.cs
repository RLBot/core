using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Conversion;

static class FlatToCommand
{
    private static readonly ILogger Logger = Logging.GetLogger("FlatToCommand");

    private static string MapGameMode(GameMode gameMode) =>
        gameMode switch
        {
            GameMode.Soccer => "?game=TAGame.GameInfo_Soccar_TA",
            GameMode.Hoops => "?game=TAGame.GameInfo_Basketball_TA",
            GameMode.Dropshot => "?game=TAGame.GameInfo_Breakout_TA",
            GameMode.Hockey => "?game=TAGame.GameInfo_Hockey_TA",
            GameMode.Rumble => "?game=TAGame.GameInfo_Items_TA",
            GameMode.Heatseeker => "?game=TAGame.GameInfo_GodBall_TA",
            GameMode.Gridiron => "?game=TAGame.GameInfo_Football_TA",
            GameMode.Knockout => "?game=TAGame.GameInfo_KnockOut_TA",
            _ => throw new ArgumentOutOfRangeException(nameof(gameMode), gameMode, null),
        };

    private static string MapMatchLength(MatchLengthMutator matchLength) =>
        matchLength switch
        {
            MatchLengthMutator.FiveMinutes => "5Minutes",
            MatchLengthMutator.TenMinutes => "10Minutes",
            MatchLengthMutator.TwentyMinutes => "20Minutes",
            MatchLengthMutator.Unlimited => "UnlimitedTime",
            _ => throw new ArgumentOutOfRangeException(nameof(matchLength), matchLength, null),
        };

    private static string MapMaxScore(MaxScoreMutator maxScore) =>
        maxScore switch
        {
            MaxScoreMutator.Unlimited => "",
            MaxScoreMutator.OneGoal => "Max1",
            MaxScoreMutator.ThreeGoals => "Max3",
            MaxScoreMutator.FiveGoals => "Max5",
            MaxScoreMutator.SevenGoals => "Max7",
            MaxScoreMutator.TenGoals => "Max10",
            MaxScoreMutator.TwentyGoals => "Max20",
            MaxScoreMutator.ThirtyGoals => "Max30",
            MaxScoreMutator.FortyGoals => "Max40",
            MaxScoreMutator.FiftyGoals => "Max50",
            MaxScoreMutator.SixtyGoals => "Max60",
            MaxScoreMutator.SeventyGoals => "Max70",
            MaxScoreMutator.EightyGoals => "Max80",
            MaxScoreMutator.NinetyGoals => "Max90",
            MaxScoreMutator.HundredGoals => "Max100",
            _ => throw new ArgumentOutOfRangeException(nameof(maxScore), maxScore, null),
        };

    private static string MapMultiBall(MultiBallMutator multiBall) =>
        multiBall switch
        {
            MultiBallMutator.One => "",
            MultiBallMutator.Two => "TwoBalls",
            MultiBallMutator.Four => "FourBalls",
            MultiBallMutator.Six => "SixBalls",
            _ => throw new ArgumentOutOfRangeException(nameof(multiBall), multiBall, null),
        };

    private static string MapOvertime(OvertimeMutator option) =>
        option switch
        {
            OvertimeMutator.Unlimited => "",
            OvertimeMutator.FiveMaxFirstScore => "Overtime5MinutesFirstScore",
            OvertimeMutator.FiveMaxRandomTeam => "Overtime5MinutesRandom",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapSeriesLength(SeriesLengthMutator option) =>
        option switch
        {
            SeriesLengthMutator.Unlimited => "",
            SeriesLengthMutator.ThreeGames => "3Games",
            SeriesLengthMutator.FiveGames => "5Games",
            SeriesLengthMutator.SevenGames => "7Games",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapGameSpeed(GameSpeedMutator option) =>
        option switch
        {
            GameSpeedMutator.Default => "",
            GameSpeedMutator.SloMo => "SloMoGameSpeed",
            GameSpeedMutator.TimeWarp => "SloMoDistanceBall",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapBallMaxSpeed(BallMaxSpeedMutator option) =>
        option switch
        {
            BallMaxSpeedMutator.Default => "",
            BallMaxSpeedMutator.Slow => "SlowBall",
            BallMaxSpeedMutator.Fast => "FastBall",
            BallMaxSpeedMutator.SuperFast => "SuperFastBall",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapBallType(BallTypeMutator option) =>
        option switch
        {
            BallTypeMutator.Default => "",
            BallTypeMutator.Cube => "Ball_CubeBall",
            BallTypeMutator.Puck => "Ball_Puck",
            BallTypeMutator.Basketball => "Ball_BasketBall",
            BallTypeMutator.Beachball => "Ball_BeachBall",
            BallTypeMutator.Anniversary => "Ball_Anniversary",
            BallTypeMutator.Haunted => "Ball_Haunted",
            BallTypeMutator.Ekin => "Ball_Ekin",
            BallTypeMutator.SpookyCube => "Ball_SpookyCube",
            BallTypeMutator.Egg => "Ball_Egg",
            BallTypeMutator.PlayerSeeking => "Ball_Fire",
            BallTypeMutator.Dropshot => "Ball_Breakout",
            BallTypeMutator.ScoreAbsorb => "Ball_Score",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapBallWeight(BallWeightMutator option) =>
        option switch
        {
            BallWeightMutator.Default => "",
            BallWeightMutator.Light => "LightBall",
            BallWeightMutator.Heavy => "HeavyBall",
            BallWeightMutator.SuperLight => "SuperLightBall",
            BallWeightMutator.CurveBall => "MagnusBall",
            BallWeightMutator.BeachBallCurve => "MagnusBeachBall",
            BallWeightMutator.MagnusFutBall => "MagnusFutBallTest",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapBallSize(BallSizeMutator option) =>
        option switch
        {
            BallSizeMutator.Default => "",
            BallSizeMutator.Small => "SmallBall",
            BallSizeMutator.Medium => "MediumBall",
            BallSizeMutator.Large => "BigBall",
            BallSizeMutator.Gigantic => "GiantBall",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapBallBounciness(BallBouncinessMutator option) =>
        option switch
        {
            BallBouncinessMutator.Default => "",
            BallBouncinessMutator.Low => "LowBounciness",
            BallBouncinessMutator.High => "HighBounciness",
            BallBouncinessMutator.SuperHigh => "SuperBounciness",
            BallBouncinessMutator.Lowish => "LowishBounciness",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapBoostAmount(BoostAmountMutator option) =>
        option switch
        {
            BoostAmountMutator.NormalBoost => "",
            BoostAmountMutator.UnlimitedBoost => "UnlimitedBooster",
            BoostAmountMutator.SlowRecharge => "SlowRecharge",
            BoostAmountMutator.RapidRecharge => "RapidRecharge",
            BoostAmountMutator.NoBoost => "NoBooster",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapRumble(RumbleMutator option) =>
        option switch
        {
            RumbleMutator.Off => "",
            RumbleMutator.DefaultRumble => "ItemsMode",
            RumbleMutator.Slow => "ItemsModeSlow",
            RumbleMutator.Civilized => "ItemsModeBallManipulators",
            RumbleMutator.DestructionDerby => "ItemsModeCarManipulators",
            RumbleMutator.SpringLoaded => "ItemsModeSprings",
            RumbleMutator.SpikesOnly => "ItemsModeSpikes",
            RumbleMutator.SpikeRush => "ItemsModeRugby",
            RumbleMutator.HauntedBallBeam => "ItemsModeHauntedBallBeam",
            RumbleMutator.Tactical => "ItemsModeSelection",
            RumbleMutator.BatmanRumble => "ItemsMode_BM",
            RumbleMutator.GrapplingOnly => "ItemsModeGrapplingHooks",
            RumbleMutator.HaymakerOnly => "ItemsModeBoxingGloves",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapBoostStrength(BoostStrengthMutator option) =>
        option switch
        {
            BoostStrengthMutator.One => "",
            BoostStrengthMutator.OneAndAHalf => "BoostMultiplier1_5x",
            BoostStrengthMutator.Two => "BoostMultiplier2x",
            BoostStrengthMutator.Five => "BoostMultiplier5x",
            BoostStrengthMutator.Ten => "BoostMultiplier10x",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapGravity(GravityMutator option) =>
        option switch
        {
            GravityMutator.Default => "",
            GravityMutator.Low => "LowGravity",
            GravityMutator.High => "HighGravity",
            GravityMutator.SuperHigh => "SuperGravity",
            GravityMutator.Reverse => "ReverseGravity",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapDemolish(DemolishMutator option) =>
        option switch
        {
            DemolishMutator.Default => "",
            DemolishMutator.Disabled => "NoDemolish",
            DemolishMutator.FriendlyFire => "DemolishAll",
            DemolishMutator.OnContact => "AlwaysDemolishOpposing",
            DemolishMutator.OnContactFF => "AlwaysDemolish",
            DemolishMutator.OnBallContact => "TeamDemoBall",
            DemolishMutator.OnBallContactFF => "FFADemoBall",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapRespawnTime(RespawnTimeMutator option) =>
        option switch
        {
            RespawnTimeMutator.ThreeSeconds => "",
            RespawnTimeMutator.TwoSeconds => "TwoSecondsRespawn",
            RespawnTimeMutator.OneSecond => "OneSecondsRespawn",
            RespawnTimeMutator.DisableGoalReset => "DisableGoalDelay",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapMaxTime(MaxTimeMutator option) =>
        option switch
        {
            MaxTimeMutator.Unlimited => "",
            MaxTimeMutator.ElevenMinutes => "MaxTime11Minutes",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapGameEvent(GameEventMutator option) =>
        option switch
        {
            GameEventMutator.Default => "",
            GameEventMutator.Rugby => "RugbyGameEventRules",
            GameEventMutator.Haunted => "HauntedGameEventRules",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapAudio(AudioMutator option) =>
        option switch
        {
            AudioMutator.Default => "",
            AudioMutator.Haunted => "HauntedAudio",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapBallGravity(BallGravityMutator option) =>
        option switch
        {
            BallGravityMutator.Default => "",
            BallGravityMutator.Low => "LowGravityBall",
            BallGravityMutator.High => "HighGravityBall",
            BallGravityMutator.SuperHigh => "SuperGravityBall",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapTerritory(TerritoryMutator option) =>
        option switch
        {
            TerritoryMutator.Off => "",
            TerritoryMutator.Territory => "Territory",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapStaleBall(StaleBallMutator option) =>
        option switch
        {
            StaleBallMutator.Unlimited => "",
            StaleBallMutator.ThirtySeconds => "ThirtySeconds",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapJump(JumpMutator option) =>
        option switch
        {
            JumpMutator.Default => "",
            JumpMutator.Grounded => "Grounded",
            JumpMutator.Two => "2DoubleJumps",
            JumpMutator.Three => "3DoubleJumps",
            JumpMutator.Four => "4DoubleJumps",
            JumpMutator.Unlimited => "UnlimitedJumps",
            JumpMutator.NoJumps => "NoJumps",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapDodgeTimer(DodgeTimerMutator option) =>
        option switch
        {
            DodgeTimerMutator.OnePointTwentyFiveSeconds => "",
            DodgeTimerMutator.TwoSeconds => "DodgeTwoSeconds",
            DodgeTimerMutator.ThreeSeconds => "DodgeThreeSeconds",
            DodgeTimerMutator.Unlimited => "DodgeUnlimitedSeconds",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapPossessionScore(PossessionScoreMutator option) =>
        option switch
        {
            PossessionScoreMutator.Off => "",
            PossessionScoreMutator.OneSecond => "Possession1Second",
            PossessionScoreMutator.TwoSeconds => "Possession2Seconds",
            PossessionScoreMutator.ThreeSeconds => "Possession3Seconds",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapDemolishScore(DemolishScoreMutator option) =>
        option switch
        {
            DemolishScoreMutator.Zero => "",
            DemolishScoreMutator.One => "DemolishScore1",
            DemolishScoreMutator.Two => "DemolishScore2",
            DemolishScoreMutator.Three => "DemolishScore3",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapNormalGoalScore(NormalGoalScoreMutator option) =>
        option switch
        {
            NormalGoalScoreMutator.One => "",
            NormalGoalScoreMutator.Zero => "GoalScore0",
            NormalGoalScoreMutator.Two => "GoalScore2",
            NormalGoalScoreMutator.Three => "GoalScore3",
            NormalGoalScoreMutator.Five => "GoalScore5",
            NormalGoalScoreMutator.Ten => "GoalScore10",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapAerialGoalScore(AerialGoalScoreMutator option) =>
        option switch
        {
            AerialGoalScoreMutator.One => "",
            AerialGoalScoreMutator.Zero => "AerialGoalScore0",
            AerialGoalScoreMutator.Two => "AerialGoalScore2",
            AerialGoalScoreMutator.Three => "AerialGoalScore3",
            AerialGoalScoreMutator.Five => "AerialGoalScore5",
            AerialGoalScoreMutator.Ten => "AerialGoalScore10",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapAssistGoalScore(AssistGoalScoreMutator option) =>
        option switch
        {
            AssistGoalScoreMutator.Zero => "",
            AssistGoalScoreMutator.One => "AssistScore1",
            AssistGoalScoreMutator.Two => "AssistScore2",
            AssistGoalScoreMutator.Three => "AssistScore3",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string MapInputRestriction(InputRestrictionMutator option) =>
        option switch
        {
            InputRestrictionMutator.Default => "",
            InputRestrictionMutator.Backwards => "BackwardsMode",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null),
        };

    private static string GetOption(string option)
    {
        if (option != "")
            return "," + option;
        return "";
    }

    public static string MakeOpenCommand(MatchConfigurationT matchConfig)
    {
        var command = "Open ";

        // Parse game map
        // With RLBot v5, GameMap enum is now ignored
        // You MUST use GameMapUpk instead
        // This is the name of the map file without the extension
        // And can also be used to tell the game to load custom maps
        if (matchConfig.GameMapUpk != "")
            command += matchConfig.GameMapUpk;
        else
        {
            command += "Stadium_P";
            Logger.LogWarning("Core got unknown map, defaulting to DFH Stadium");
        }

        // Parse game mode
        command += MapGameMode(matchConfig.GameMode);

        if (matchConfig.SkipReplays)
            command += "?noreplay";

        // Whether to or not to skip the kickoff countdown
        if (!matchConfig.InstantStart)
            command += "?Playtest";

        command += "?GameTags=PlayerCount8";

        if (matchConfig.Freeplay)
            command += ",Freeplay";

        if (matchConfig.Mutators is not { } mutatorSettings)
            return command;

        // Parse mutator settings
        command += GetOption(MapMatchLength(mutatorSettings.MatchLength));
        command += GetOption(MapMaxScore(mutatorSettings.MaxScore));
        command += GetOption(MapMultiBall(mutatorSettings.MultiBall));
        command += GetOption(MapOvertime(mutatorSettings.Overtime));
        command += GetOption(MapSeriesLength(mutatorSettings.SeriesLength));
        command += GetOption(MapGameSpeed(mutatorSettings.GameSpeed));
        command += GetOption(MapBallMaxSpeed(mutatorSettings.BallMaxSpeed));
        command += GetOption(MapBallType(mutatorSettings.BallType));
        command += GetOption(MapBallWeight(mutatorSettings.BallWeight));
        command += GetOption(MapBallSize(mutatorSettings.BallSize));
        command += GetOption(MapBallBounciness(mutatorSettings.BallBounciness));
        command += GetOption(MapBoostAmount(mutatorSettings.BoostAmount));
        command += GetOption(MapRumble(mutatorSettings.Rumble));
        command += GetOption(MapBoostStrength(mutatorSettings.BoostStrength));
        command += GetOption(MapGravity(mutatorSettings.Gravity));
        command += GetOption(MapDemolish(mutatorSettings.Demolish));
        command += GetOption(MapRespawnTime(mutatorSettings.RespawnTime));
        command += GetOption(MapMaxTime(mutatorSettings.MaxTime));
        command += GetOption(MapGameEvent(mutatorSettings.GameEvent));
        command += GetOption(MapAudio(mutatorSettings.Audio));
        command += GetOption(MapBallGravity(mutatorSettings.BallGravity));
        command += GetOption(MapTerritory(mutatorSettings.Territory));
        command += GetOption(MapStaleBall(mutatorSettings.StaleBall));
        command += GetOption(MapJump(mutatorSettings.Jump));
        command += GetOption(MapDodgeTimer(mutatorSettings.DodgeTimer));
        command += GetOption(MapPossessionScore(mutatorSettings.PossessionScore));
        command += GetOption(MapDemolishScore(mutatorSettings.DemolishScore));
        command += GetOption(MapNormalGoalScore(mutatorSettings.NormalGoalScore));
        command += GetOption(MapAerialGoalScore(mutatorSettings.AerialGoalScore));
        command += GetOption(MapAssistGoalScore(mutatorSettings.AssistGoalScore));

        return command;
    }

    public static string MakeGameSpeedCommand(float gameSpeed) =>
        "Set WorldInfo TimeDilation " + gameSpeed;

    public static string MakeGravityCommand(float gravity) =>
        "Set WorldInfo WorldGravityZ " + gravity;

    public static string MakeAutoSaveReplayCommand() => "QueSaveReplay";
}
