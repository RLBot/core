using rlbot.flat;

namespace RLBotCS.Conversion
{
    internal class FlatToCommand
    {
        static public string MakeOpenCommand(MatchSettingsT matchSettings)
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
            command += "?game=";
            switch (matchSettings.GameMode)
            {
                case GameMode.Soccer:
                    command += "TAGame.GameInfo_Soccar_TA";
                    break;
                default:
                    command += "TAGame.GameInfo_Soccar_TA";
                    Console.WriteLine("Core got unknown game mode, defaulting to Soccer");
                    break;
            }

            // Whether to or not to skip the kickoff countdown
            if (!matchSettings.InstantStart)
            {
                command += "?Playtest";
            }

            // Parse mutator settings
            command += "?GameTags=";

            List<string> game_tags = ["PlayerCount8"];

            if (matchSettings.MutatorSettings is MutatorSettingsT mutatorSettings)
            {
                switch (mutatorSettings.MatchLength)
                {
                    case MatchLength.Five_Minutes:
                        break;
                    case MatchLength.Unlimited:
                        game_tags.Add("UnlimitedTime");
                        break;
                    default:
                        Console.WriteLine("Got got unsupported match length option");
                        break;
                }
            }

            command += String.Join(",", game_tags);

            return command;
        }

        static public string MakeGameSpeedCommand(float gameSpeed)
        {
            return "Set WorldInfo TimeDilation " + gameSpeed.ToString();
        }

        static public string MakeGameSpeedCommandFromOption(GameSpeedOption gameSpeed)
        {
            return MakeGameSpeedCommand(gameSpeed switch
            {
                GameSpeedOption.Slo_Mo => 0.5f,
                GameSpeedOption.Time_Warp => 1.5f,
                _ => 1.0f,
            });
        }

        static public string MakeGravityCommand(float gravity)
        {
            return "Set WorldInfo WorldGravityZ " + gravity.ToString();
        }

        static public string MakeGravityCommandFromOption(GravityOption gravityOption)
        {
            return MakeGravityCommand(gravityOption switch
            {
                GravityOption.Low => -325,
                GravityOption.High => -1137.5f,
                GravityOption.Super_High => -3250,
                _ => -650,
            });
        }

        static public string MakeAutoSaveReplayCommand()
        {
            return "QueSaveReplay";
        }
    }
}