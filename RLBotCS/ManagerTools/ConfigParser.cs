using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.Conversion;
using Tomlyn;
using Tomlyn.Model;

namespace RLBotCS.ManagerTools;

public static class ConfigParser
{
    private static readonly ILogger Logger = Logging.GetLogger("ConfigParser");

    private static TomlTable GetTable(string? path)
    {
        if (path == null)
        {
            Logger.LogError("Could not read Toml file, path is null");
            return [];
        }

        try
        {
            // TODO - catch any exceptions thrown by ToModel
            return Toml.ToModel(File.ReadAllText(path));
        }
        catch (FileNotFoundException)
        {
            Logger.LogError($"Could not find Toml file at '{path}'");
            return [];
        }
    }

    // GetTable retrieves a TomlTable from a file. ParseTable retrieves a table within another table

    private static TomlTable ParseTable(
        TomlTable table,
        string key,
        List<string> missingValues
    )
    {
        try
        {
            return (TomlTable)table[key];
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return [];
        }
    }

    private static TomlTableArray ParseTableArray(
        TomlTable table,
        string key,
        List<string> missingValues
    )
    {
        try
        {
            return (TomlTableArray)table[key];
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return [];
        }
    }

    // Get the enum value of a given enum and the string name of the desired key
    private static T ParseEnum<T>(
        TomlTable table,
        string key,
        T fallback,
        List<string> missingValues
    )
        where T : struct, Enum
    {
        try
        {
            if (Enum.TryParse((string)table[key], true, out T value))
                return value;

            Logger.LogError($"'{key}' has invalid value, using default setting instead");
            return fallback;
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return fallback;
        }
    }

    private static int ParseInt(
        TomlTable table,
        string key,
        int fallback,
        List<string> missingValues
    )
    {
        try
        {
            return (int)(long)table[key];
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return fallback;
        }
    }

    private static uint ParseUint(
        TomlTable table,
        string key,
        uint fallback,
        List<string> missingValues
    )
    {
        try
        {
            return (uint)(long)table[key];
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return fallback;
        }
    }

    private static float ParseFloat(
        TomlTable table,
        string key,
        float fallback,
        List<string> missingValues
    )
    {
        try
        {
            return Convert.ToSingle(table[key]);
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return fallback;
        }
    }

    private static string? ParseString(
        TomlTable table,
        string key,
        string? fallback,
        List<string> missingValues
    )
    {
        try
        {
            return (string)table[key];
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return fallback;
        }
    }

    private static string? CombinePaths(string? parent, string? child)
    {
        if (parent == null || child == null)
            return null;

        return Path.Combine(parent, child);
    }

    private static bool ParseBool(
        TomlTable table,
        string key,
        bool fallback,
        List<string> missingValues
    )
    {
        try
        {
            return (bool)table[key];
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return fallback;
        }
    }

    private static string GetRunCommand(TomlTable runnableSettings, List<string> missingValues)
    {
        string? runCommandWindows = ParseString(
            runnableSettings,
            "run_command",
            null,
            missingValues
        );
        string? runCommandLinux = ParseString(
            runnableSettings,
            "run_command_linux",
            null,
            missingValues
        );

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return runCommandWindows ?? "";

        if (runCommandLinux != null)
            return runCommandLinux;

        // TODO:
        // We're currently on Linux but there's no Linux-specific run command
        // Try running the Windows command under Wine instead
        Logger.LogError("No Linux-specific run command found for script!");
        return runCommandWindows ?? "";
    }

    private static ScriptConfigurationT GetScriptConfig(
        TomlTable scriptTable,
        string playerTomlPath,
        List<string> missingValues
    )
    {
        string? scriptTomlPath = ParseString(scriptTable, "config", null, missingValues);
        TomlTable scriptToml = GetTable(CombinePaths(playerTomlPath, scriptTomlPath));
        string tomlParent = Path.GetDirectoryName(scriptTomlPath) ?? "";

        ScriptConfigurationT scriptConfig =
            new()
            {
                Location = CombinePaths(
                    tomlParent,
                    ParseString(scriptToml, "location", "", missingValues)
                ),
                RunCommand = GetRunCommand(scriptToml, missingValues)
            };
        return scriptConfig;
    }

    private static PlayerConfigurationT GetPlayerConfig(
        TomlTable table,
        string matchConfigPath,
        List<string> missingValues
    ) =>
        ParseEnum(table, "type", PlayerClass.RLBot, missingValues) switch
        {
            PlayerClass.RLBot
                => GetBotConfig(
                    table,
                    PlayerClassUnion.FromRLBot(new RLBotT()),
                    matchConfigPath,
                    missingValues
                ),
            PlayerClass.Human
                => GetHumanConfig(
                    table,
                    PlayerClassUnion.FromHuman(new HumanT()),
                    missingValues
                ),
            PlayerClass.Psyonix
                => GetPsyonixConfig(
                    table,
                    PlayerClassUnion.FromPsyonix(
                        new PsyonixT
                        {
                            BotSkill = ParseFloat(table, "skill", 1.0f, missingValues)
                        }
                    ),
                    missingValues
                ),
            PlayerClass.PartyMember
                => throw new NotImplementedException("PartyMember not implemented"),
            _ => throw new NotImplementedException("Unimplemented PlayerClass type")
        };

    private static PlayerConfigurationT GetHumanConfig(
        TomlTable table,
        PlayerClassUnion classUnion,
        List<string> missingValues
    ) =>
        new()
        {
            Variety = classUnion,
            Team = ParseUint(table, "team", 0, missingValues),
            Name = "Human",
            Location = "",
            RunCommand = ""
        };

    private static PlayerConfigurationT GetPsyonixConfig(
        TomlTable table,
        PlayerClassUnion classUnion,
        List<string> missingValues
    )
    {
        var team = ParseUint(table, "team", 0, missingValues);
        var (fullName, preset) = PsyonixPresets.GetRandom((int)team);

        var namePrefix = classUnion.AsPsyonix().BotSkill switch
        {
            < 0 => "Beginner ",
            < 0.5f => "Rookie ",
            < 1 => "Pro ",
            _ => ""
        };

        return new()
        {
            Variety = classUnion,
            Team = team,
            Name = namePrefix + fullName.Split('_')[1],
            Location = "",
            RunCommand = "",
            Loadout = preset,
        };
    }

    private static PlayerLoadoutT? GetPlayerLoadout(
        TomlTable playerTable,
        string? tomlParent,
        List<string> missingValues
    )
    {
        string? loadoutTomlPath = CombinePaths(
            tomlParent,
            ParseString(playerTable, "looks_config", null, missingValues)
        );

        if (loadoutTomlPath == null)
            return null;

        TomlTable loadoutToml = GetTable(loadoutTomlPath);

        string teamLoadoutString =
            ParseInt(playerTable, "team", 0, missingValues) == 0
                ? "blue_loadout"
                : "orange_loadout";
        TomlTable teamLoadout = ParseTable(loadoutToml, teamLoadoutString, missingValues);
        TomlTable teamPaint = ParseTable(teamLoadout, "paint", missingValues);

        return new PlayerLoadoutT()
        {
            TeamColorId = ParseUint(teamLoadout, "team_color_id", 0, missingValues),
            CustomColorId = ParseUint(teamLoadout, "custom_color_id", 0, missingValues),
            CarId = ParseUint(teamLoadout, "car_id", 0, missingValues),
            DecalId = ParseUint(teamLoadout, "decal_id", 0, missingValues),
            WheelsId = ParseUint(teamLoadout, "wheels_id", 0, missingValues),
            BoostId = ParseUint(teamLoadout, "boost_id", 0, missingValues),
            AntennaId = ParseUint(teamLoadout, "antenna_id", 0, missingValues),
            HatId = ParseUint(teamLoadout, "hat_id", 0, missingValues),
            PaintFinishId = ParseUint(teamLoadout, "paint_finish_id", 0, missingValues),
            CustomFinishId = ParseUint(teamLoadout, "custom_finish_id", 0, missingValues),
            EngineAudioId = ParseUint(teamLoadout, "engine_audio_id", 0, missingValues),
            TrailsId = ParseUint(teamLoadout, "trails_id", 0, missingValues),
            GoalExplosionId = ParseUint(teamLoadout, "goal_explosion_id", 0, missingValues),
            LoadoutPaint = new LoadoutPaintT()
            {
                CarPaintId = ParseUint(teamPaint, "car_paint_id", 0, missingValues),
                DecalPaintId = ParseUint(teamPaint, "decal_paint_id", 0, missingValues),
                WheelsPaintId = ParseUint(teamPaint, "wheels_paint_id", 0, missingValues),
                BoostPaintId = ParseUint(teamPaint, "boost_paint_id", 0, missingValues),
                AntennaPaintId = ParseUint(teamPaint, "antenna_paint_id", 0, missingValues),
                HatPaintId = ParseUint(teamPaint, "hat_paint_id", 0, missingValues),
                TrailsPaintId = ParseUint(teamPaint, "trails_paint_id", 0, missingValues),
                GoalExplosionPaintId = ParseUint(
                    teamPaint,
                    "goal_explosion_paint_id",
                    0,
                    missingValues
                ),
            },
            // TODO - GetPrimary/Secondary color? Do any bots use this?
        };
    }

    private static PlayerConfigurationT GetBotConfig(
        TomlTable rlbotPlayerTable,
        PlayerClassUnion classUnion,
        string matchConfigPath,
        List<string> missingValues
    )
    {
        /*
         * rlbotPlayerTable is the "bot" table in rlbot.toml. Contains team, path to bot.toml, and more
         * "playerToml" is the entire bot.toml file
         * "playerSettings" is the "settings" table in bot.toml. Contains name, directory, appearance path, etc
         * "playerDetails" is the "details" table in bot.toml. Contains the fun facts about the bot
         * "loadoutToml" is the entire bot_looks.toml. Contains appearance for orange and blue team
         *  "teamLoadout" is either the "blue_loadout" or "orange_loadout" in bot_looks.toml, contains player items
         *  "teamPaint" is the "paint" table within the loadout tables, contains paint colors of player items
         */
        string? matchConfigParent = Path.GetDirectoryName(matchConfigPath);

        string? playerTomlPath = CombinePaths(
            matchConfigParent,
            ParseString(rlbotPlayerTable, "config", null, missingValues)
        );
        TomlTable playerToml = GetTable(CombinePaths(matchConfigParent, playerTomlPath));
        string? tomlParent = Path.GetDirectoryName(playerTomlPath);

        TomlTable playerSettings = ParseTable(playerToml, "settings", missingValues);

        return new PlayerConfigurationT
        {
            Variety = classUnion,
            Team = ParseUint(rlbotPlayerTable, "team", 0, missingValues),
            Name = ParseString(playerSettings, "name", "Unnamed RLBot", missingValues),
            Location = CombinePaths(
                tomlParent,
                ParseString(playerSettings, "location", "", missingValues)
            ),
            RunCommand = GetRunCommand(playerSettings, missingValues),
            Loadout = GetPlayerLoadout(playerSettings, tomlParent, missingValues),
            Hivemind = ParseBool(playerSettings, "hivemind", false, missingValues),
        };
    }

    private static MutatorSettingsT GetMutatorSettings(
        TomlTable mutatorTable,
        List<string> missingValues
    ) =>
        new MutatorSettingsT()
        {
            MatchLength = ParseEnum(
                mutatorTable,
                "match_length",
                MatchLength.Five_Minutes,
                missingValues
            ),
            MaxScore = ParseEnum(mutatorTable, "max_score", MaxScore.Default, missingValues),
            MultiBall = ParseEnum(mutatorTable, "multi_ball", MultiBall.One, missingValues),
            OvertimeOption = ParseEnum(
                mutatorTable,
                "overtime",
                OvertimeOption.Unlimited,
                missingValues
            ),
            GameSpeedOption = ParseEnum(
                mutatorTable,
                "game_speed",
                GameSpeedOption.Default,
                missingValues
            ),
            BallMaxSpeedOption = ParseEnum(
                mutatorTable,
                "ball_max_speed",
                BallMaxSpeedOption.Default,
                missingValues
            ),
            BallTypeOption = ParseEnum(
                mutatorTable,
                "ball_type",
                BallTypeOption.Default,
                missingValues
            ),
            BallWeightOption = ParseEnum(
                mutatorTable,
                "ball_weight",
                BallWeightOption.Default,
                missingValues
            ),
            BallSizeOption = ParseEnum(
                mutatorTable,
                "ball_size",
                BallSizeOption.Default,
                missingValues
            ),
            BallBouncinessOption = ParseEnum(
                mutatorTable,
                "ball_bounciness",
                BallBouncinessOption.Default,
                missingValues
            ),
            BoostOption = ParseEnum(
                mutatorTable,
                "boost_amount",
                BoostOption.Normal_Boost,
                missingValues
            ),
            RumbleOption = ParseEnum(
                mutatorTable,
                "rumble",
                RumbleOption.No_Rumble,
                missingValues
            ),
            BoostStrengthOption = ParseEnum(
                mutatorTable,
                "boost_strength",
                BoostStrengthOption.One,
                missingValues
            ),
            GravityOption = ParseEnum(
                mutatorTable,
                "gravity",
                GravityOption.Default,
                missingValues
            ),
            DemolishOption = ParseEnum(
                mutatorTable,
                "demolish",
                DemolishOption.Default,
                missingValues
            ),
            RespawnTimeOption = ParseEnum(
                mutatorTable,
                "respawn_time",
                RespawnTimeOption.Three_Seconds,
                missingValues
            ),
        };

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

        Dictionary<string, List<string>> missingValues = new();
        missingValues[""] = [];
        missingValues["rlbot"] = [];
        missingValues["match"] = [];
        missingValues["mutators"] = [];
        missingValues["cars"] = [];
        missingValues["scripts"] = [];

        TomlTable rlbotTable = ParseTable(rlbotToml, "rlbot", missingValues[""]);
        TomlTable matchTable = ParseTable(rlbotToml, "match", missingValues[""]);
        TomlTable mutatorTable = ParseTable(rlbotToml, "mutators", missingValues[""]);
        TomlTableArray players = ParseTableArray(rlbotToml, "cars", missingValues[""]);
        TomlTableArray scripts = ParseTableArray(rlbotToml, "scripts", missingValues[""]);

        List<PlayerConfigurationT> playerConfigs = [];
        // Gets the PlayerConfigT object for the number of players requested
        int numBots = ParseInt(matchTable, "num_cars", 0, missingValues["match"]);
        for (int i = 0; i < Math.Min(numBots, players.Count); i++)
            playerConfigs.Add(GetPlayerConfig(players[i], path, missingValues["cars"]));

        List<ScriptConfigurationT> scriptConfigs = [];
        int numScripts = ParseInt(matchTable, "num_scripts", 0, missingValues["match"]);
        for (int i = 0; i < Math.Min(numScripts, scripts.Count); i++)
            scriptConfigs.Add(GetScriptConfig(scripts[i], path, missingValues["scripts"]));

        var matchSettings = new MatchSettingsT
        {
            Launcher = ParseEnum(
                rlbotTable,
                "launcher",
                Launcher.Steam,
                missingValues["rlbot"]
            ),
            AutoStartBots = ParseBool(
                rlbotTable,
                "auto_start_bots",
                true,
                missingValues["rlbot"]
            ),
            GamePath = ParseString(
                rlbotTable,
                "rocket_league_exe_path",
                "",
                missingValues["rlbot"]
            ),
            GameMode = ParseEnum(
                matchTable,
                "game_mode",
                GameMode.Soccer,
                missingValues["match"]
            ),
            GameMapUpk = ParseString(
                matchTable,
                "game_map_upk",
                "Stadium_P",
                missingValues["match"]
            ),
            SkipReplays = ParseBool(matchTable, "skip_replays", false, missingValues["match"]),
            InstantStart = ParseBool(
                matchTable,
                "start_without_countdown",
                false,
                missingValues["match"]
            ),
            EnableRendering = ParseBool(
                matchTable,
                "enable_rendering",
                false,
                missingValues["match"]
            ),
            EnableStateSetting = ParseBool(
                matchTable,
                "enable_state_setting",
                true,
                missingValues["match"]
            ),
            ExistingMatchBehavior = ParseEnum(
                matchTable,
                "existing_match_behavior",
                ExistingMatchBehavior.Restart,
                missingValues["match"]
            ),
            AutoSaveReplay = ParseBool(
                matchTable,
                "auto_save_replay",
                false,
                missingValues["match"]
            ),
            Freeplay = ParseBool(matchTable, "freeplay", false, missingValues["match"]),
            MutatorSettings = GetMutatorSettings(mutatorTable, missingValues["mutators"]),
            PlayerConfigurations = playerConfigs,
            ScriptConfigurations = scriptConfigs
        };

        if (missingValues.Count > 0)
        {
            string missingValuesString = string.Join(
                ", ",
                missingValues.SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}.{v}"))
            );
            Logger.LogWarning($"Missing values in toml: {missingValuesString}");
        }

        return matchSettings;
    }
}
