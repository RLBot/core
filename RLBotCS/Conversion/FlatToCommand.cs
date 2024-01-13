using rlbot.flat;

namespace RLBotCS.Conversion
{
    internal class FlatToCommand
    {
        // I apologise to anyone who has to modify this formatting - ddthj
        // He yelled at me when I tried to modify the formatting - Virx

        static string[] gameModeKeyNames =
        [
            /*Soccar*/"?game=TAGame.GameInfo_Soccar_TA",
            /*Hoops*/"?game=TAGame.GameInfo_Basketball_TA",
            /*DropShot*/"?game=TAGame.GameInfo_Breakout_TA",
            /*Hockey*/"?game=TAGame.GameInfo_Hockey_TA",
            /*Rumble*/"?game=TAGame.GameInfo_Items_TA",
            /*Heatseeker*/"?game=TAGame.GameInfo_GodBall_TA",
            /*Gridiron*/"?game=TAGame.GameInfo_Football_TA",
        ];

        static string[] mapMatchLengthNames =
        [
            /*Five_Minutes*/"5Minutes",
            /*Ten_Minutes*/"10Minutes",
            /*Twenty_Minutes*/"20Minutes",
            /*Unlimited*/"UnlimitedTime"
        ];

        static string[] maxScoreOptionNames =
        [
            /*Unlimited*/"",
            /*1 Goal*/"Max1",
            /*3 Goals*/"Max3",
            /*5 Goals*/"Max5"
        ];

        static string[] overtimeOptionNames =
        [
            /*Unlimited*/"",
            /*+5 Max, First Score*/"Overtime5MinutesFirstScore",
            /*+5 Max, Random Team*/"Overtime5MinutesRandom"
        ];

        static string[] seriesLengthOptionNames =
        [
            /*Unlimited*/"",
            /*3 Games*/"3Games",
            /*5 Games*/"5Games",
            /*7 Games*/"7Games"
        ];

        static string[] gameSpeedOptionNames =
        [
            /*Default*/"",
            /*Slo-Mo*/"SloMoGameSpeed",
            /*Time Warp*/"SloMoDistanceBall"
        ];

        static string[] ballMaxSpeedOptionNames =
        [
            /*Default*/"",
            /*Slow*/"SlowBall",
            /*Fast*/"FastBall",
            /*Super Fast*/"SuperFastBall"
        ];

        static string[] ballTypeOptionNames =
        [
            /*Default*/"",
            /*Cube*/"Ball_CubeBall",
            /*Puck*/"Ball_Puck",
            /*Basketball*/"Ball_BasketBall"
        ];

        static string[] ballWeightOptionNames =
        [
            /*Default*/"",
            /*Light*/"LightBall",
            /*Heavy*/"HeavyBall",
            /*Super Light*/"SuperLightBall"
        ];

        static string[] ballSizeOptionNames =
        [
            /*Default*/"",
            /*Small*/"SmallBall",
            /*Large*/"BigBall",
            /*Gigantic*/"GiantBall"
        ];

        static string[] ballBouncinessOptionNames =
        [
            /*Default*/"",
            /*Low*/"LowBounciness",
            /*High*/"HighBounciness",
            /*Super High*/"SuperBounciness"
        ];

        static string[] boostOptionNames =
        [
            /*Normal_Boost*/"",
            /*Unlimited_Boost*/"UnlimitedBooster",
            /*Slow_Recharge*/"SlowRecharge",
            /*Rapid_Recharge*/"RapidRecharge",
            /*No_Boost*/"NoBooster"
        ];

        static string[] rumbleOptionNames =
        [
            /*None*/"",
            /*Default*/"ItemsMode",
            /*Slow*/"ItemsModeSlow",
            /*Civilized*/"ItemsModeBallManipulators",
            /*Destruction Derby*/"ItemsModeCarManipulators",
            /*Spring Loaded*/"ItemsModeSprings",
            /*Spikes Only*/"ItemsModeSpikes",
            /*Spike Rush*/"ItemsModeRugby"
        ];

        static string[] boostStrengthOptionNames =
        [
            /*1x*/"",
            /*1.5x*/"BoostMultiplier1_5x",
            /*2x*/"BoostMultiplier2x",
            /*10x*/"BoostMultiplier10x"
        ];

        static string[] gravityOptionNames =
        [
            /*Default*/"",
            /*Low*/"LowGravity",
            /*High*/"HighGravity",
            /*Super High*/"SuperGravity"
        ];

        static string[] demolishOptionNames =
        [
            /*Default*/"",
            /*Disabled*/"NoDemolish",
            /*Friendly Fire*/"DemolishAll",
            /*On Contact*/"AlwaysDemolishOpposing",
            /*On Contact (FF)*/"AlwaysDemolish"
        ];

        static string[] respawnTimeOptionNames =
        [
            /*3 Seconds*/"",
            /*2 Seconds*/"TwoSecondsRespawn",
            /*1 Second*/"OneSecondsRespawn",
            /*Disable Goal Reset*/"DisableGoalDelay"
        ];

        static string GetOption(string option)
        {
            if (option != "")
            {
                return "," + option;
            }
            return "";
        }

        public static string MakeOpenCommand(MatchSettingsT matchSettings)
        {
            var command = "Open ";

            // Parse game map
            // With RLBot v5, GameMap enum is now ignored
            // You MUST use GameMapUpk instead
            // This is the name of the map file without the extension
            // And can also be used to tell the game to load custom maps
            if (matchSettings.GameMapUpk != "")
            {
                command += matchSettings.GameMapUpk;
            }
            else
            {
                command += "Stadium_P";
                Console.WriteLine("Core got unknown map, defaulting to DFH Stadium");
            }

            // Parse game mode
            command += gameModeKeyNames[(int)matchSettings.GameMode];

            // Whether to or not to skip the kickoff countdown
            if (!matchSettings.InstantStart)
            {
                command += "?Playtest";
            }

            // Parse mutator settings
            command += "?GameTags=PlayerCount8";
            if (matchSettings.MutatorSettings is MutatorSettingsT mutatorSettings)
            {
                command += GetOption(mapMatchLengthNames[(int)mutatorSettings.MatchLength]);
                command += GetOption(maxScoreOptionNames[(int)mutatorSettings.MaxScore]);
                command += GetOption(overtimeOptionNames[(int)mutatorSettings.OvertimeOption]);
                command += GetOption(seriesLengthOptionNames[(int)mutatorSettings.SeriesLengthOption]);
                command += GetOption(gameSpeedOptionNames[(int)mutatorSettings.GameSpeedOption]);
                command += GetOption(ballMaxSpeedOptionNames[(int)mutatorSettings.BallMaxSpeedOption]);
                command += GetOption(ballTypeOptionNames[(int)mutatorSettings.BallTypeOption]);
                command += GetOption(ballWeightOptionNames[(int)mutatorSettings.BallWeightOption]);
                command += GetOption(ballSizeOptionNames[(int)mutatorSettings.BallSizeOption]);
                command += GetOption(ballBouncinessOptionNames[(int)mutatorSettings.BallBouncinessOption]);
                command += GetOption(boostOptionNames[(int)mutatorSettings.BoostOption]);
                command += GetOption(rumbleOptionNames[(int)mutatorSettings.RumbleOption]); //TODO - probably doesn't work
                command += GetOption(boostStrengthOptionNames[(int)mutatorSettings.BoostStrengthOption]);
                command += GetOption(gravityOptionNames[(int)mutatorSettings.GravityOption]);
                command += GetOption(demolishOptionNames[(int)mutatorSettings.DemolishOption]);
                command += GetOption(respawnTimeOptionNames[(int)mutatorSettings.RespawnTimeOption]);
            }

            return command;
        }

        public static string MakeGameSpeedCommand(float gameSpeed)
        {
            return "Set WorldInfo TimeDilation " + gameSpeed.ToString();
        }

        public static string MakeGameSpeedCommandFromOption(GameSpeedOption gameSpeed)
        {
            return MakeGameSpeedCommand(
                gameSpeed switch
                {
                    GameSpeedOption.Slo_Mo => 0.5f,
                    GameSpeedOption.Time_Warp => 1.5f,
                    _ => 1.0f,
                }
            );
        }

        public static string MakeGravityCommand(float gravity)
        {
            return "Set WorldInfo WorldGravityZ " + gravity.ToString();
        }

        public static string MakeGravityCommandFromOption(GravityOption gravityOption)
        {
            return MakeGravityCommand(
                gravityOption switch
                {
                    GravityOption.Low => -325,
                    GravityOption.High => -1137.5f,
                    GravityOption.Super_High => -3250,
                    _ => -650,
                }
            );
        }

        public static string MakeAutoSaveReplayCommand()
        {
            return "QueSaveReplay";
        }
    }
}
