using rlbot.flat;
using Tomlyn.Model;
using Tomlyn;

namespace MatchConfigManager
{
    public class ConfigParser
    {
        public TomlTable tomlConfig = [];
        public bool tomlLoaded = false;

        public void LoadConfigFile(string path)
        {
            string file = File.ReadAllText(path);
            tomlConfig = Toml.ToModel(file);
            tomlLoaded = true;
        }

        static public T ParseEnum<T>(TomlTable table, string key, T fallback) where T : struct, Enum
        {
            if (Enum.TryParse((string)table[key], out T value)) {
                return value;
            }
            else
            {
                Console.WriteLine($"Warning! Unable to read {0}, using default instead", key);
                return fallback;
            }
        }

        public void GetMatchSettings(string path, MatchSettingsT matchSettings)
        {
            // TODO - there is little childproofing here. Will children ever use the toml file?
            if (!tomlLoaded)
            {
                LoadConfigFile(path);
            }

            TomlTable settings = (TomlTable)tomlConfig["match"];

            int num_bots = (int)(long)settings["num_participants"];

            matchSettings.GameMode = ParseEnum(settings, "game_mode", GameMode.Soccer);
            matchSettings.GameMapUpk = (string)settings["game_map_upk"];
            matchSettings.SkipReplays = (bool)settings["skip_replays"];
            matchSettings.InstantStart = (bool)settings["start_without_countdown"];
            matchSettings.EnableRendering = (bool)settings["enable_rendering"];
            matchSettings.EnableStateSetting = (bool)settings["enable_state_setting"];
            matchSettings.ExistingMatchBehavior = ParseEnum(settings, "existing_match_behavior", ExistingMatchBehavior.Restart_If_Different);
            matchSettings.AutoSaveReplay = (bool)settings["auto_save_replay"];

            TomlTable mutators = (TomlTable)tomlConfig["mutators"];

            matchSettings.MutatorSettings = new MutatorSettingsT()
            {
                MatchLength = ParseEnum(mutators, "match_length", MatchLength.Five_Minutes),
                MaxScore = ParseEnum(mutators, "max_score", MaxScore.Unlimited),
                OvertimeOption = ParseEnum(mutators, "overtime", OvertimeOption.Unlimited),
                GameSpeedOption = ParseEnum(mutators, "game_speed", GameSpeedOption.Default),
                BallMaxSpeedOption = ParseEnum(mutators, "ball_max_speed", BallMaxSpeedOption.Default),
                BallTypeOption = ParseEnum(mutators, "ball_type", BallTypeOption.Default),
                BallWeightOption = ParseEnum(mutators, "ball_weight", BallWeightOption.Default),
                BallSizeOption = ParseEnum(mutators, "ball_size", BallSizeOption.Default),
                BallBouncinessOption = ParseEnum(mutators, "ball_bounciness", BallBouncinessOption.Default),
                BoostOption = ParseEnum(mutators, "boost_amount", BoostOption.Normal_Boost),
                RumbleOption = ParseEnum(mutators, "rumble", RumbleOption.Default),
                BoostStrengthOption = ParseEnum(mutators, "boost_strength", BoostStrengthOption.One),
                GravityOption = ParseEnum(mutators, "gravity", GravityOption.Default),
                DemolishOption = ParseEnum(mutators, "demolish", DemolishOption.Default),
                RespawnTimeOption = ParseEnum(mutators, "respawn_time", RespawnTimeOption.Three_Seconds),
            };

            TomlTableArray bots = (TomlTableArray)tomlConfig["bots"];
            Console.WriteLine($"Bots array len: {0}", bots.Count);
            
            List<PlayerConfigurationT> playerConfigs = [];
            
            foreach (TomlTable bot in bots){
                playerConfigs.Add(new PlayerConfigurationT()
                {
                    // TODO - load bot.toml to get all info!
                    Team = (int)(long)bot["team"], //We have to cast to long first because that is how Toml parses it
                    Variety = (string)bot["type"],
                    
                });
            }
        }
    }
}
