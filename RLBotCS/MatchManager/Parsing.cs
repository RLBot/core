using rlbot.flat;
using Tomlyn.Model;
using Tomlyn;
using System.ComponentModel.DataAnnotations;
using RLBotModels.Message;
using RLBotCS.Conversion;

namespace MatchConfigManager
{
    public class ConfigParser
    {
        public TomlTable tomlConfig = [];
        public bool tomlLoaded = false;

        public static TomlTable GetTable(string path)
        {
            try
            {
                //TODO - catch any exceptions thrown by ToModel
                return Toml.ToModel(File.ReadAllText(path));
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Warning! Could not read Toml file at '{0}'", path);
                return [];
            }
        }
        //GetTable retrieves a TomlTable from a file. ParseTable retrieves a table within another table

        public static TomlTable ParseTable(TomlTable table, string key)
        {
            try
            {
                return (TomlTable)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"Warning! Could not find the '{0}' table!", key);
                return [];
            }
        }

        //Generic to get the enum value of a given enum and the string name of the desired key
        static public T ParseEnum<T>(TomlTable table, string key, T fallback) where T : struct, Enum
        {
            if (Enum.TryParse((string)table[key], out T value)) {
                return value;
            }
            else
            {
                Console.WriteLine($"Warning! Unable to read '{0}', using default setting instead", key);
                return fallback;
            }
        }

        static public int ParseInt(TomlTable table, string key, int fallback)
        {
            try
            {
                return (int)(long)table[key];
            }
            catch (KeyNotFoundException) {
                Console.WriteLine($"Could not find the '{0}' field in toml. Using default setting '{1}' instead", key, fallback);
                return fallback;
            }
        }

        static public float ParseFloat(TomlTable table, string key, float fallback)
        {
            try
            {
                return (float)(double)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"Could not find the '{0}' field in toml. Using default setting '{1}' instead", key, fallback);
                return fallback;
            }
        }

        static public string ParseString(TomlTable table, string key,  string fallback)
        {
            try
            {
                return (string)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"Could not find the '{0}' field in toml. Using default setting '{1}' instead", key, fallback);
                return fallback;
            }
        }

        static public bool ParseBool(TomlTable table, string key, bool fallback)
        {
            try
            {
                return (bool)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"Could not find the '{0}' field in toml. Using default setting '{1}' instead", key, fallback);
                return fallback;
            }
        }

        //This stupid union is just because psyonix players have one extra property - sleep-deprived ddthj 
        public static PlayerClassUnion GetPlayerUnion(TomlTable player)
        {
            PlayerClassUnion playerClassUnion;
            PlayerClass playerClass = ParseEnum(player, "type", PlayerClass.PsyonixBotPlayer);

            switch (playerClass)
            {
                case PlayerClass.RLBotPlayer:
                    playerClassUnion = PlayerClassUnion.FromRLBotPlayer(new RLBotPlayerT());
                    break;
                case PlayerClass.HumanPlayer:
                    playerClassUnion = PlayerClassUnion.FromHumanPlayer(new HumanPlayerT());
                    break;
                case PlayerClass.PsyonixBotPlayer:
                    float botSkill = ParseFloat(player, "skill", 1.0f);
                    playerClassUnion = PlayerClassUnion.FromPsyonixBotPlayer(new PsyonixBotPlayerT() { BotSkill = botSkill});
                    break;
                case PlayerClass.PartyMemberBotPlayer:
                    playerClassUnion = PlayerClassUnion.FromPartyMemberBotPlayer(new PartyMemberBotPlayerT());
                    Console.WriteLine("TODO - PartyMemeberBots are not implemented");
                    break;
                default:
                    playerClassUnion = PlayerClassUnion.FromHumanPlayer(new HumanPlayerT());
                    //TODO - this is lazy
                    Console.WriteLine("Warning! Could not determine player type, spawning human instead");
                    break;
            }
            playerClassUnion.Value = playerClass;

            return playerClassUnion;
        }

        public static PlayerConfigurationT GetPlayerConfig(TomlTable player)
        {
            //Fetch the bot.toml file
            TomlTable playerTable = GetTable(ParseString(player, "path", "PathNotReadable"));

            //Fetch the 'settings' table inside bot.toml
            TomlTable playerSettings = ParseTable(playerTable, "settings");
            string path = ParseString(playerSettings, "bot_starter", "PathNotReadable");
            int maxTickRate = ParseInt(playerSettings, "max_tick_rate_preference", 120);

            //Now that we have the tables with the necessary information, we begin creation of the playerConfig
            PlayerConfigurationT playerConfig = new()
            {
                Variety = GetPlayerUnion(player),   //Contains type and skill
                Team = ParseInt(player, "team", 0),
                Name = ParseString(playerSettings, "name", "NameNotReadable"),
            };

            //Fetching the bot_appearance.toml
            TomlTable playerLoadout = GetTable(ParseString(playerSettings, "looks_config", "PathNotReadable"));
            playerConfig.Loadout = new PlayerLoadoutT()
            {
                TeamColorId = ParseInt(playerLoadout, "team_color_id", 0),
                CustomColorId = ParseInt(playerLoadout, "custom_color_id", 0),
                CarId = ParseInt(playerLoadout, "car_id", 0),
                DecalId = ParseInt(playerLoadout, "decal_id", 0),
                WheelsId = ParseInt(playerLoadout, "wheels_id", 0),
                BoostId = ParseInt(playerLoadout, "boost_id", 0),
                AntennaId = ParseInt(playerLoadout, "antenna_id", 0),
                HatId = ParseInt(playerLoadout, "hat_id", 0),
                PaintFinishId = ParseInt(playerLoadout, "paint_finish_id", 0),
                CustomFinishId = ParseInt(playerLoadout, "custom_finish_id", 0),
                EngineAudioId = ParseInt(playerLoadout, "engine_audio_id", 0),
                TrailsId = ParseInt(playerLoadout, "trails_id", 0),
                GoalExplosionId = ParseInt(playerLoadout, "goal_explosion_id", 0),
                LoadoutPaint = new LoadoutPaintT()
                {
                    CarPaintId = ParseInt(playerLoadout, "car_paint_id", 0),
                    DecalPaintId = ParseInt(playerLoadout, "decal_paint_id", 0),
                    WheelsPaintId = ParseInt(playerLoadout, "wheels_paint_id", 0),
                    BoostPaintId = ParseInt(playerLoadout, "boost_paint_id", 0),
                    AntennaPaintId = ParseInt(playerLoadout, "antenna_paint_id", 0),
                    HatPaintId = ParseInt(playerLoadout, "hat_paint_id", 0),
                    TrailsPaintId = ParseInt(playerLoadout, "trails_paint_id", 0),
                    GoalExplosionPaintId = ParseInt(playerLoadout, "goal_explosion_paint_id", 0),
                },
                // TODO - GetPrimary/Secondary color? Do any bots use this?
            };            

            //Fetching the fun details from bot.toml
            //TODO - unused
            TomlTable playerDetails = ParseTable(playerTable, "details");
            string description = ParseString(playerDetails, "description", "");
            string funFact = ParseString(playerDetails, "fun_fact", "");
            string developer = ParseString(playerDetails, "developer", "");
            string language = ParseString(playerDetails, "language", "");
            // List<string> tags = (List<string>)playerDetails["tags"]; //Currently no childproofed method for array

            return playerConfig;
        }

        public MatchSettingsT GetMatchSettings(string path)
        {

            if (!tomlLoaded)
            {
                tomlConfig = GetTable(path);
                tomlLoaded = true;
            }

            MatchSettingsT matchSettings = new();

            TomlTable settings = ParseTable(tomlConfig, "match");

            int num_bots = ParseInt(settings, "num_participants", 2);

            matchSettings.GameMode = ParseEnum(settings, "game_mode", rlbot.flat.GameMode.Soccer);
            matchSettings.GameMapUpk = ParseString(settings, "game_map_upk", "Stadium_P");
            matchSettings.SkipReplays = ParseBool(settings, "skip_replays", false);
            matchSettings.InstantStart = ParseBool(settings, "start_without_countdown", false);
            matchSettings.EnableRendering = ParseBool(settings, "enable_rendering", false);
            matchSettings.EnableStateSetting = ParseBool(settings, "enable_state_setting", false);
            matchSettings.ExistingMatchBehavior = ParseEnum(settings, "existing_match_behavior", ExistingMatchBehavior.Restart_If_Different);
            matchSettings.AutoSaveReplay = ParseBool(settings, "auto_save_replay", false);

            TomlTable mutators = ParseTable(tomlConfig, "mutators");

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

            //TODO - not childproof
            TomlTableArray players = (TomlTableArray)tomlConfig["bots"];
            Console.WriteLine($"Bots array len: {0}", players.Count);
            
            List<PlayerConfigurationT> playerConfigs = [];
            
            foreach (TomlTable player in players){
                playerConfigs.Add(GetPlayerConfig(player));
            }

            matchSettings.PlayerConfigurations = playerConfigs;
            return matchSettings;
        }
    }
}
