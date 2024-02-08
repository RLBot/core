using rlbot.flat;

namespace RLBotCS.Conversion
{
    internal class FlatToCommand
    {
        static string MapGameMode(GameMode gameMode) =>
            gameMode switch
            {
                GameMode.Soccer => "?game=TAGame.GameInfo_Soccar_TA",
                GameMode.Hoops => "?game=TAGame.GameInfo_Basketball_TA",
                GameMode.Dropshot => "?game=TAGame.GameInfo_Breakout_TA",
                GameMode.Hockey => "?game=TAGame.GameInfo_Hockey_TA",
                GameMode.Rumble => "?game=TAGame.GameInfo_Items_TA",
                GameMode.Heatseeker => "?game=TAGame.GameInfo_GodBall_TA",
                GameMode.Gridiron => "?game=TAGame.GameInfo_Football_TA",
                _ => throw new ArgumentOutOfRangeException(nameof(gameMode), gameMode, null)
            };

        static string MapMatchLength(MatchLength matchLength) =>
            matchLength switch
            {
                MatchLength.Five_Minutes => "5Minutes",
                MatchLength.Ten_Minutes => "10Minutes",
                MatchLength.Twenty_Minutes => "20Minutes",
                MatchLength.Unlimited => "UnlimitedTime",
                _ => throw new ArgumentOutOfRangeException(nameof(matchLength), matchLength, null)
            };

        static string MapMaxScore(MaxScore maxScore) =>
            maxScore switch
            {
                MaxScore.Unlimited => "",
                MaxScore.One_Goal => "Max1",
                MaxScore.Three_Goals => "Max3",
                MaxScore.Five_Goals => "Max5",
                _ => throw new ArgumentOutOfRangeException(nameof(maxScore), maxScore, null)
            };

        static string MapOvertime(OvertimeOption option) =>
            option switch
            {
                OvertimeOption.Unlimited => "",
                OvertimeOption.Five_Max_First_Score => "Overtime5MinutesFirstScore",
                OvertimeOption.Five_Max_Random_Team => "Overtime5MinutesRandom",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapSeriesLength(SeriesLengthOption option) =>
            option switch
            {
                SeriesLengthOption.Unlimited => "",
                SeriesLengthOption.Three_Games => "3Games",
                SeriesLengthOption.Five_Games => "5Games",
                SeriesLengthOption.Seven_Games => "7Games",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapGameSpeed(GameSpeedOption option) =>
            option switch
            {
                GameSpeedOption.Default => "",
                GameSpeedOption.Slo_Mo => "SloMoGameSpeed",
                GameSpeedOption.Time_Warp => "SloMoDistanceBall",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapBallMaxSpeed(BallMaxSpeedOption option) =>
            option switch
            {
                BallMaxSpeedOption.Default => "",
                BallMaxSpeedOption.Slow => "SlowBall",
                BallMaxSpeedOption.Fast => "FastBall",
                BallMaxSpeedOption.Super_Fast => "SuperFastBall",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapBallType(BallTypeOption option) =>
            option switch
            {
                BallTypeOption.Default => "",
                BallTypeOption.Cube => "Ball_CubeBall",
                BallTypeOption.Puck => "Ball_Puck",
                BallTypeOption.Basketball => "Ball_BasketBall",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapBallWeight(BallWeightOption option) =>
            option switch
            {
                BallWeightOption.Default => "",
                BallWeightOption.Light => "LightBall",
                BallWeightOption.Heavy => "HeavyBall",
                BallWeightOption.Super_Light => "SuperLightBall",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapBallSize(BallSizeOption option) =>
            option switch
            {
                BallSizeOption.Default => "",
                BallSizeOption.Small => "SmallBall",
                BallSizeOption.Large => "BigBall",
                BallSizeOption.Gigantic => "GiantBall",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapBallBounciness(BallBouncinessOption option) =>
            option switch
            {
                BallBouncinessOption.Default => "",
                BallBouncinessOption.Low => "LowBounciness",
                BallBouncinessOption.High => "HighBounciness",
                BallBouncinessOption.Super_High => "SuperBounciness",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapBoost(BoostOption option) =>
            option switch
            {
                BoostOption.Normal_Boost => "",
                BoostOption.Unlimited_Boost => "UnlimitedBooster",
                BoostOption.Slow_Recharge => "SlowRecharge",
                BoostOption.Rapid_Recharge => "RapidRecharge",
                BoostOption.No_Boost => "NoBooster",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapRumble(RumbleOption option) =>
            option switch
            {
                RumbleOption.No_Rumble => "",
                RumbleOption.Default => "ItemsMode",
                RumbleOption.Slow => "ItemsModeSlow",
                RumbleOption.Civilized => "ItemsModeBallManipulators",
                RumbleOption.Destruction_Derby => "ItemsModeCarManipulators",
                RumbleOption.Spring_Loaded => "ItemsModeSprings",
                RumbleOption.Spikes_Only => "ItemsModeSpikes",
                RumbleOption.Spike_Rush => "ItemsModeRugby",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapBoostStrength(BoostStrengthOption option) =>
            option switch
            {
                BoostStrengthOption.One => "",
                BoostStrengthOption.OneAndAHalf => "BoostMultiplier1_5x",
                BoostStrengthOption.Two => "BoostMultiplier2x",
                BoostStrengthOption.Ten => "BoostMultiplier10x",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapGravity(GravityOption option) =>
            option switch
            {
                GravityOption.Default => "",
                GravityOption.Low => "LowGravity",
                GravityOption.High => "HighGravity",
                GravityOption.Super_High => "SuperGravity",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapDemolish(DemolishOption option) =>
            option switch
            {
                DemolishOption.Default => "",
                DemolishOption.Disabled => "NoDemolish",
                DemolishOption.Friendly_Fire => "DemolishAll",
                DemolishOption.On_Contact => "AlwaysDemolishOpposing",
                DemolishOption.On_Contact_FF => "AlwaysDemolish",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string MapRespawnTime(RespawnTimeOption option) =>
            option switch
            {
                RespawnTimeOption.Three_Seconds => "",
                RespawnTimeOption.Two_Seconds => "TwoSecondsRespawn",
                RespawnTimeOption.One_Seconds => "OneSecondsRespawn",
                RespawnTimeOption.Disable_Goal_Reset => "DisableGoalDelay",
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

        static string GetOption(string option)
        {
            if (option != "")
                return "," + option;
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
            command += MapGameMode(matchSettings.GameMode);

            // Whether to or not to skip the kickoff countdown
            if (!matchSettings.InstantStart)
            {
                command += "?Playtest";
            }

            // Parse mutator settings
            command += "?GameTags=PlayerCount8";
            if (matchSettings.MutatorSettings is MutatorSettingsT mutatorSettings)
            {
                command += GetOption(MapMatchLength(mutatorSettings.MatchLength));
                command += GetOption(MapMaxScore(mutatorSettings.MaxScore));
                command += GetOption(MapOvertime(mutatorSettings.OvertimeOption));
                command += GetOption(MapSeriesLength(mutatorSettings.SeriesLengthOption));
                command += GetOption(MapGameSpeed(mutatorSettings.GameSpeedOption));
                command += GetOption(MapBallMaxSpeed(mutatorSettings.BallMaxSpeedOption));
                command += GetOption(MapBallType(mutatorSettings.BallTypeOption));
                command += GetOption(MapBallWeight(mutatorSettings.BallWeightOption));
                command += GetOption(MapBallSize(mutatorSettings.BallSizeOption));
                command += GetOption(MapBallBounciness(mutatorSettings.BallBouncinessOption));
                command += GetOption(MapBoost(mutatorSettings.BoostOption));
                command += GetOption(MapRumble(mutatorSettings.RumbleOption)); //TODO - probably doesn't work
                command += GetOption(MapBoostStrength(mutatorSettings.BoostStrengthOption));
                command += GetOption(MapGravity(mutatorSettings.GravityOption));
                command += GetOption(MapDemolish(mutatorSettings.DemolishOption));
                command += GetOption(MapRespawnTime(mutatorSettings.RespawnTimeOption));
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