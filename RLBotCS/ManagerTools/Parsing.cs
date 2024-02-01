using rlbot.flat;
using Tomlyn;
using Tomlyn.Model;

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
                Console.WriteLine($"Warning! Could not read Toml file at '{path}'");
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
                Console.WriteLine($"Warning! Could not find the '{key}' table!");
                return [];
            }
        }

        public static TomlTableArray ParseTableArray(TomlTable table, string key)
        {
            try
            {
                return (TomlTableArray)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"Warning! Could not find the '{key}' table!");
                return [];
            }
        }

        //Generic to get the enum value of a given enum and the string name of the desired key
        static public T ParseEnum<T>(TomlTable table, string key, T fallback)
            where T : struct, Enum
        {
            if (Enum.TryParse((string)table[key], true, out T value))
            {
                return value;
            }
            else
            {
                Console.WriteLine($"Warning! Unable to read '{key}', using default setting instead");
                return fallback;
            }
        }

        public static int ParseInt(TomlTable table, string key, int fallback)
        {
            try
            {
                return (int)(long)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine(
                    $"Could not find the '{key}' field in toml. Using default setting '{fallback}' instead"
                );
                return fallback;
            }
        }

        public static uint ParseUint(TomlTable table, string key, uint fallback)
        {
            try
            {
                return (uint)(long)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine(
                    $"Could not find the '{key}' field in toml. Using default setting '{fallback}' instead"
                );
                return fallback;
            }
        }

        public static float ParseFloat(TomlTable table, string key, float fallback)
        {
            try
            {
                return (float)(double)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine(
                    $"Could not find the '{key}' field in toml. Using default setting '{fallback}' instead"
                );
                return fallback;
            }
        }

        public static string ParseString(TomlTable table, string key, string fallback)
        {
            try
            {
                return (string)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine(
                    $"Could not find the '{key}' field in toml. Using default setting '{fallback}' instead"
                );
                return fallback;
            }
        }

        public static bool ParseBool(TomlTable table, string key, bool fallback)
        {
            try
            {
                return (bool)table[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine(
                    $"Could not find the '{key}' field in toml. Using default setting '{fallback}' instead"
                );
                return fallback;
            }
        }

        private static ScriptConfigurationT GetScriptConfig(TomlTable scriptTable)
        {
            string scriptTomlPath = ParseString(scriptTable, "config", "");
            TomlTable scriptToml = GetTable(scriptTomlPath);
            string tomlParent = Path.GetDirectoryName(scriptTomlPath) ?? "";

            ScriptConfigurationT scriptConfig =
                new()
                {
                    Location = Path.Combine(tomlParent, ParseString(scriptToml, "location", "")),
                    RunCommand = ParseString(scriptToml, "run_command", "")
                };
            return scriptConfig;
        }

        public static PlayerConfigurationT GetPlayerConfig(TomlTable rlbotPlayerTable)
        {
            PlayerClassUnion playerClassUnion;
            PlayerClass playerClass = ParseEnum(rlbotPlayerTable, "type", PlayerClass.Psyonix);

            switch (playerClass)
            {
                case PlayerClass.RLBot:
                    playerClassUnion = PlayerClassUnion.FromRLBot(new RLBotT());
                    return GetBotConfig(rlbotPlayerTable, playerClassUnion);
                case PlayerClass.Human:
                    playerClassUnion = PlayerClassUnion.FromHuman(new HumanT());
                    return GetHumanConfig(rlbotPlayerTable, playerClassUnion);
                case PlayerClass.Psyonix:
                    float botSkill = ParseFloat(rlbotPlayerTable, "skill", 1.0f);
                    playerClassUnion = PlayerClassUnion.FromPsyonix(new PsyonixT() { BotSkill = botSkill });
                    return GetPsyonixConfig(rlbotPlayerTable, playerClassUnion);
                case PlayerClass.PartyMember:
                    // playerClassUnion = PlayerClassUnion.FromPartyMember(new PartyMemberT());
                    throw new NotImplementedException("PartyMemeberBots are not implemented");
                default:
                    throw new NotImplementedException("Bot type not implemented... How did we get here???");
            }
        }

        public static PlayerConfigurationT GetHumanConfig(TomlTable rlbotPlayerTable, PlayerClassUnion classUnion)
        {
            PlayerConfigurationT playerConfig =
                new()
                {
                    Variety = classUnion,
                    Team = ParseUint(rlbotPlayerTable, "team", 0),
                    Name = "",
                    Location = "",
                    RunCommand = ""
                };

            return playerConfig;
        }

        public static PlayerConfigurationT GetPsyonixConfig(
            TomlTable rlbotPlayerTable,
            PlayerClassUnion classUnion
        )
        {
            // TODO - support psyonix bot loadouts
            PlayerConfigurationT playerConfig =
                new()
                {
                    Variety = classUnion,
                    Team = ParseUint(rlbotPlayerTable, "team", 0),
                    Name = "",
                    Location = "",
                    RunCommand = ""
                };

            return playerConfig;
        }

        public static PlayerConfigurationT GetBotConfig(TomlTable rlbotPlayerTable, PlayerClassUnion classUnion)
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

            string playerTomlPath = ParseString(rlbotPlayerTable, "config", "");
            TomlTable playerToml = GetTable(playerTomlPath);
            string tomlParent = Path.GetDirectoryName(playerTomlPath) ?? "";

            TomlTable playerSettings = ParseTable(playerToml, "settings");
            TomlTable loadoutToml = GetTable(ParseString(playerSettings, "looks_config", ""));
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

            PlayerConfigurationT playerConfig =
                new()
                {
                    Variety = classUnion,
                    Team = ParseUint(rlbotPlayerTable, "team", 0),
                    Name = ParseString(playerSettings, "name", ""),
                    Location = Path.Combine(tomlParent, ParseString(playerSettings, "location", "")),
                    RunCommand = ParseString(playerSettings, "run_command", ""),
                    Loadout = new PlayerLoadoutT()
                    {
                        TeamColorId = ParseUint(teamLoadout, "team_color_id", 0),
                        CustomColorId = ParseUint(teamLoadout, "custom_color_id", 0),
                        CarId = ParseUint(teamLoadout, "car_id", 0),
                        DecalId = ParseUint(teamLoadout, "decal_id", 0),
                        WheelsId = ParseUint(teamLoadout, "wheels_id", 0),
                        BoostId = ParseUint(teamLoadout, "boost_id", 0),
                        AntennaId = ParseUint(teamLoadout, "antenna_id", 0),
                        HatId = ParseUint(teamLoadout, "hat_id", 0),
                        PaintFinishId = ParseUint(teamLoadout, "paint_finish_id", 0),
                        CustomFinishId = ParseUint(teamLoadout, "custom_finish_id", 0),
                        EngineAudioId = ParseUint(teamLoadout, "engine_audio_id", 0),
                        TrailsId = ParseUint(teamLoadout, "trails_id", 0),
                        GoalExplosionId = ParseUint(teamLoadout, "goal_explosion_id", 0),
                        LoadoutPaint = new LoadoutPaintT()
                        {
                            CarPaintId = ParseUint(teamPaint, "car_paint_id", 0),
                            DecalPaintId = ParseUint(teamPaint, "decal_paint_id", 0),
                            WheelsPaintId = ParseUint(teamPaint, "wheels_paint_id", 0),
                            BoostPaintId = ParseUint(teamPaint, "boost_paint_id", 0),
                            AntennaPaintId = ParseUint(teamPaint, "antenna_paint_id", 0),
                            HatPaintId = ParseUint(teamPaint, "hat_paint_id", 0),
                            TrailsPaintId = ParseUint(teamPaint, "trails_paint_id", 0),
                            GoalExplosionPaintId = ParseUint(teamPaint, "goal_explosion_paint_id", 0),
                        },
                        // TODO - GetPrimary/Secondary color? Do any bots use this?
                    }
                };

            return playerConfig;
        }

        public static MatchSettingsT GetMatchSettings(string path)
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
            TomlTableArray players = ParseTableArray(rlbotToml, "cars");
            TomlTableArray scripts = ParseTableArray(rlbotToml, "scripts");

            List<PlayerConfigurationT> playerConfigs = [];
            List<ScriptConfigurationT> scriptConfigs = [];
            MatchSettingsT matchSettings =
                new()
                {
                    Launcher = ParseEnum(rlbotTable, "launcher", Launcher.Steam),
                    AutoStartBots = ParseBool(rlbotTable, "auto_start_bots", true),
                    GamePath = ParseString(rlbotTable, "rocket_league_exe_path", ""),
                    GameMode = ParseEnum(matchTable, "game_mode", GameMode.Soccer),
                    GameMapUpk = ParseString(matchTable, "game_map_upk", "Stadium_P"),
                    SkipReplays = ParseBool(matchTable, "skip_replays", false),
                    InstantStart = ParseBool(matchTable, "start_without_countdown", false),
                    EnableRendering = ParseBool(matchTable, "enable_rendering", false),
                    EnableStateSetting = ParseBool(matchTable, "enable_state_setting", false),
                    ExistingMatchBehavior = ParseEnum(
                        matchTable,
                        "existing_match_behavior",
                        ExistingMatchBehavior.Restart_If_Different
                    ),
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
                        BallBouncinessOption = ParseEnum(
                            mutatorTable,
                            "ball_bounciness",
                            BallBouncinessOption.Default
                        ),
                        BoostOption = ParseEnum(mutatorTable, "boost_amount", BoostOption.Normal_Boost),
                        RumbleOption = ParseEnum(mutatorTable, "rumble", RumbleOption.Default),
                        BoostStrengthOption = ParseEnum(mutatorTable, "boost_strength", BoostStrengthOption.One),
                        GravityOption = ParseEnum(mutatorTable, "gravity", GravityOption.Default),
                        DemolishOption = ParseEnum(mutatorTable, "demolish", DemolishOption.Default),
                        RespawnTimeOption = ParseEnum(
                            mutatorTable,
                            "respawn_time",
                            RespawnTimeOption.Three_Seconds
                        ),
                    }
                };

            // Gets the PlayerConfigT object for the number of players requested
            int num_bots = ParseInt(matchTable, "num_cars", 0);
            for (int i = 0; i < Math.Min(num_bots, players.Count); i++)
            {
                playerConfigs.Add(GetPlayerConfig(players[i]));
            }
            matchSettings.PlayerConfigurations = playerConfigs;

            int num_scripts = ParseInt(matchTable, "num_scripts", 0);
            for (int i = 0; i < Math.Min(num_scripts, scripts.Count); i++)
            {
                scriptConfigs.Add(GetScriptConfig(scripts[i]));
            }

            matchSettings.ScriptConfigurations = scriptConfigs;

            return matchSettings;
        }
    }
}
