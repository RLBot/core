using rlbot.flat;
using Tomlyn.Model;
using Tomlyn;

namespace MatchManagement
{
    public class ConfigParser
    {
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
        public static PlayerClassUnion GetPlayerUnion(TomlTable rlbotPlayerTable)
        {
            PlayerClassUnion playerClassUnion;
            PlayerClass playerClass = ParseEnum(rlbotPlayerTable, "type", PlayerClass.PsyonixBotPlayer);

            switch (playerClass)
            {
                case PlayerClass.RLBotPlayer:
                    playerClassUnion = PlayerClassUnion.FromRLBotPlayer(new RLBotPlayerT());
                    break;
                case PlayerClass.HumanPlayer:
                    playerClassUnion = PlayerClassUnion.FromHumanPlayer(new HumanPlayerT());
                    break;
                case PlayerClass.PsyonixBotPlayer:
                    float botSkill = ParseFloat(rlbotPlayerTable, "skill", 1.0f);
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

        public static PlayerConfigurationT GetPlayerConfig(TomlTable rlbotPlayerTable)
        {
            /* 
             * rlbotPlayerTable is the the "bot" table in rlbot.toml. Contains team, path to bot.toml, and more
             * "playerToml" is the entire bot.toml file
             * "playerSettings" is the "settings" table in bot.toml. Contains name, directory, appearance path, etc
             * "playerDetails" is the "details" table in bot.toml. Contains the fun facts about the bot
             * "loadoutToml" is the entire bot_looks.toml. Contains appearance for orange and blue team
             *  "teamLoadout" is either the "blue_loadout" or "orange_loadout" in bot_looks.toml, contains player items
             *  "teamPaint" is the "paint" table within the loadout tables, contains paint colors of player items
             */
            
            TomlTable playerToml = GetTable(ParseString(rlbotPlayerTable, "config", "BotPathUnreadable"));
            TomlTable playerSettings = ParseTable(playerToml, "settings");
            TomlTable loadoutToml = GetTable(ParseString(playerSettings, "looks_config", "LooksPathNotReadable"));
            TomlTable teamLoadout;

            if (ParseInt(rlbotPlayerTable, "team", 0) == 0)
            {
                teamLoadout = ParseTable(loadoutToml, "blue_loadout");
            }
            else
            {
                teamLoadout = ParseTable(loadoutToml, "orange_loadout");
            }

            TomlTable teamPaint = ParseTable(teamLoadout, "paint");

            PlayerConfigurationT playerConfig = new()
            {
                Variety = GetPlayerUnion(rlbotPlayerTable),   //Contains type and psyonix skill
                Team = ParseInt(rlbotPlayerTable, "team", 0),
                Name = ParseString(playerSettings, "name", "NameNotReadable"),
                Location = ParseString(playerSettings, "location", "PathNotReadable"),
                RunCommand = ParseString(playerSettings, "run_command", ""),
                Loadout = new PlayerLoadoutT()
                {
                    TeamColorId = ParseInt(teamLoadout, "team_color_id", 0),
                    CustomColorId = ParseInt(teamLoadout, "custom_color_id", 0),
                    CarId = ParseInt(teamLoadout, "car_id", 0),
                    DecalId = ParseInt(teamLoadout, "decal_id", 0),
                    WheelsId = ParseInt(teamLoadout, "wheels_id", 0),
                    BoostId = ParseInt(teamLoadout, "boost_id", 0),
                    AntennaId = ParseInt(teamLoadout, "antenna_id", 0),
                    HatId = ParseInt(teamLoadout, "hat_id", 0),
                    PaintFinishId = ParseInt(teamLoadout, "paint_finish_id", 0),
                    CustomFinishId = ParseInt(teamLoadout, "custom_finish_id", 0),
                    EngineAudioId = ParseInt(teamLoadout, "engine_audio_id", 0),
                    TrailsId = ParseInt(teamLoadout, "trails_id", 0),
                    GoalExplosionId = ParseInt(teamLoadout, "goal_explosion_id", 0),
                    LoadoutPaint = new LoadoutPaintT()
                    {
                        CarPaintId = ParseInt(teamPaint, "car_paint_id", 0),
                        DecalPaintId = ParseInt(teamPaint, "decal_paint_id", 0),
                        WheelsPaintId = ParseInt(teamPaint, "wheels_paint_id", 0),
                        BoostPaintId = ParseInt(teamPaint, "boost_paint_id", 0),
                        AntennaPaintId = ParseInt(teamPaint, "antenna_paint_id", 0),
                        HatPaintId = ParseInt(teamPaint, "hat_paint_id", 0),
                        TrailsPaintId = ParseInt(teamPaint, "trails_paint_id", 0),
                        GoalExplosionPaintId = ParseInt(teamPaint, "goal_explosion_paint_id", 0),
                    },
                    // TODO - GetPrimary/Secondary color? Do any bots use this?
                }
            };

            return playerConfig;
        }

        public MatchSettingsT GetMatchSettings(string path)
        {
            /* 
             * "rlbotToml" is the entire rlbot.toml file
             * "rlbotTable" is the "rlbot" table in rlbot.toml. It contains rlbot-specific settings like game launch options
             * "matchTable" is the "match" table in rlbot.toml. It contains match-specific matchTable like the map
             * "mutatorTable" is the "mutators" table in rlbot.toml. It contains the match mutators
             * "players" is the list of "bot" tables in rlbot.toml
             * "playerToml" is the "bot" table in rlbot.toml. It contains the path to the bot.toml file
             */
            TomlTable rlbotToml = GetTable(path);
            TomlTable rlbotTable = ParseTable(rlbotToml, "rlbot");
            TomlTable matchTable = ParseTable(rlbotToml, "match");
            TomlTable mutatorTable = ParseTable(rlbotToml, "mutators");
            TomlTableArray players = (TomlTableArray)rlbotToml["bots"]; //TODO - not childproof
            Console.WriteLine($"Bots array len: {0}", players.Count);

            List<PlayerConfigurationT> playerConfigs = [];
            MatchSettingsT matchSettings = new()
            {
                Launcher = ParseEnum(rlbotTable, "launcher", Launcher.Steam),
                AutoStartBots = ParseBool(rlbotTable, "auto_start_bots", true),
                GameMode = ParseEnum(matchTable, "game_mode", rlbot.flat.GameMode.Soccer),
                GameMapUpk = ParseString(matchTable, "game_map_upk", "Stadium_P"),
                SkipReplays = ParseBool(matchTable, "skip_replays", false),
                InstantStart = ParseBool(matchTable, "start_without_countdown", false),
                EnableRendering = ParseBool(matchTable, "enable_rendering", false),
                EnableStateSetting = ParseBool(matchTable, "enable_state_setting", false),
                ExistingMatchBehavior = ParseEnum(matchTable, "existing_match_behavior", ExistingMatchBehavior.Restart_If_Different),
                AutoSaveReplay = ParseBool(matchTable, "auto_save_replay", false),
                MutatorSettings = new MutatorSettingsT()
                {
                    MatchLength = ParseEnum(mutatorTable, "match_length", MatchLength.Five_Minutes),
                    MaxScore = ParseEnum(mutatorTable, "max_score", MaxScore.Unlimited),
                    OvertimeOption = ParseEnum(mutatorTable, "overtime", OvertimeOption.Unlimited),
                    GameSpeedOption = ParseEnum(mutatorTable, "game_speed", GameSpeedOption.Default),
                    BallMaxSpeedOption = ParseEnum(mutatorTable, "ball_max_speed", BallMaxSpeedOption.Default),
                    BallTypeOption = ParseEnum(mutatorTable, "ball_type", BallTypeOption.Default),
                    BallWeightOption = ParseEnum(mutatorTable, "ball_weight", BallWeightOption.Default),
                    BallSizeOption = ParseEnum(mutatorTable, "ball_size", BallSizeOption.Default),
                    BallBouncinessOption = ParseEnum(mutatorTable, "ball_bounciness", BallBouncinessOption.Default),
                    BoostOption = ParseEnum(mutatorTable, "boost_amount", BoostOption.Normal_Boost),
                    RumbleOption = ParseEnum(mutatorTable, "rumble", RumbleOption.Default),
                    BoostStrengthOption = ParseEnum(mutatorTable, "boost_strength", BoostStrengthOption.One),
                    GravityOption = ParseEnum(mutatorTable, "gravity", GravityOption.Default),
                    DemolishOption = ParseEnum(mutatorTable, "demolish", DemolishOption.Default),
                    RespawnTimeOption = ParseEnum(mutatorTable, "respawn_time", RespawnTimeOption.Three_Seconds),
                }
            };

            // Gets the PlayerConfigT object for the number of players requested
            int num_bots = ParseInt(matchTable, "num_participants", 0);
            for (int i = 0; i < Math.Min(num_bots,players.Count); i++){
                playerConfigs.Add(GetPlayerConfig(players[i]));
            }
            matchSettings.PlayerConfigurations = playerConfigs;

            return matchSettings;
        }
    }
}
