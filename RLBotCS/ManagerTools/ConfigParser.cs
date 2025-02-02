﻿using System.Runtime.InteropServices;
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

    private static string? ParseString(TomlTable table, string key, List<string> missingValues)
    {
        try
        {
            return (string)table[key];
        }
        catch (KeyNotFoundException)
        {
            missingValues.Add(key);
            return null;
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
        string runCommandWindows =
            ParseString(runnableSettings, "run_command", missingValues) ?? "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return runCommandWindows;

        string runCommandLinux =
            ParseString(runnableSettings, "run_command_linux", missingValues) ?? "";

        if (runCommandLinux != "")
            return runCommandLinux;

        if (runCommandWindows != "")
        {
            // TODO:
            // We're currently on Linux but there's no Linux-specific run command
            // Try running the Windows command under Wine instead
            Logger.LogError("No Linux-specific run command found for script!");
            return runCommandWindows;
        }

        // No run command found
        return "";
    }

    private static ScriptConfigurationT GetScriptConfig(
        TomlTable scriptTable,
        string matchConfigPath,
        List<string> missingValues
    )
    {
        string? matchConfigParent = Path.GetDirectoryName(matchConfigPath);

        string? scriptTomlPath = CombinePaths(
            matchConfigParent,
            ParseString(scriptTable, "config", missingValues)
        );
        TomlTable scriptToml = GetTable(scriptTomlPath);
        string tomlParent = Path.GetDirectoryName(scriptTomlPath) ?? "";

        TomlTable scriptSettings = ParseTable(scriptToml, "settings", missingValues);

        string name = ParseString(scriptSettings, "name", missingValues) ?? "Unnamed Script";
        string agentId =
            ParseString(scriptSettings, "agent_id", missingValues) ?? $"script/{name}";

        ScriptConfigurationT scriptConfig = new()
        {
            Name = name,
            RootDir = CombinePaths(
                tomlParent,
                ParseString(scriptSettings, "root_dir", missingValues) ?? ""
            ),
            RunCommand = GetRunCommand(scriptSettings, missingValues),
            AgentId = agentId,
        };
        return scriptConfig;
    }

    private static PlayerConfigurationT GetPlayerConfig(
        TomlTable table,
        string matchConfigPath,
        List<string> missingValues
    ) =>
        ParseEnum(table, "type", PlayerClass.CustomBot, missingValues) switch
        {
            PlayerClass.CustomBot => GetBotConfig(
                table,
                PlayerClassUnion.FromCustomBot(new CustomBotT()),
                matchConfigPath,
                missingValues
            ),
            PlayerClass.Human => GetHumanConfig(
                table,
                PlayerClassUnion.FromHuman(new HumanT()),
                missingValues
            ),
            PlayerClass.Psyonix => GetPsyonixConfig(
                table,
                PlayerClassUnion.FromPsyonix(
                    new PsyonixT
                    {
                        BotSkill = ParseEnum(
                            table,
                            "skill",
                            PsyonixSkill.AllStar,
                            missingValues
                        ),
                    }
                ),
                matchConfigPath,
                missingValues
            ),
            PlayerClass.PartyMember => throw new NotImplementedException(
                "PartyMember not implemented"
            ),
            _ => throw new NotImplementedException("Unimplemented PlayerClass type"),
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
            RootDir = "",
            RunCommand = "",
            AgentId = "",
        };

    private static PlayerConfigurationT GetPsyonixConfig(
        TomlTable playerTable,
        PlayerClassUnion classUnion,
        string matchConfigPath,
        List<string> missingValues
    )
    {
        string? nameOverride = ParseString(playerTable, "name", missingValues);
        string? loadoutPathOverride = ParseString(playerTable, "loadout_file", missingValues);

        string? matchConfigParent = Path.GetDirectoryName(matchConfigPath);

        string? playerTomlPath = CombinePaths(
            matchConfigParent,
            ParseString(playerTable, "config", missingValues)
        );
        TomlTable playerToml = GetTable(playerTomlPath);
        string? tomlParent = Path.GetDirectoryName(playerTomlPath);

        TomlTable playerSettings = ParseTable(playerToml, "settings", missingValues);

        uint team = ParseUint(playerTable, "team", 0, missingValues);
        string? name = nameOverride ?? ParseString(playerSettings, "name", missingValues);
        PlayerLoadoutT? loadout = GetPlayerLoadout(
            playerSettings,
            loadoutPathOverride,
            team,
            tomlParent,
            missingValues
        );

        if (name == null)
        {
            (name, var presetLoadout) = PsyonixLoadouts.GetNext((int)team);
            loadout ??= presetLoadout;
        }
        else if (loadout == null && PsyonixLoadouts.GetFromName(name, (int)team) is { } presetLoadout)
        {
            loadout = presetLoadout;
        }

        string agentId =
            ParseString(playerSettings, "agent_id", missingValues) ?? $"psyonix/{name}";
        string runCommand = GetRunCommand(playerSettings, missingValues);
        string rootDir = "";

        if (runCommand != "")
        {
            rootDir =
                CombinePaths(
                    tomlParent,
                    ParseString(playerSettings, "root_dir", missingValues)
                ) ?? "";
        }

        return new()
        {
            Variety = classUnion,
            Team = team,
            Name = name,
            RootDir = rootDir,
            RunCommand = runCommand,
            Loadout = loadout,
            AgentId = agentId,
        };
    }

    private static PlayerLoadoutT? GetPlayerLoadout(
        TomlTable playerTable,
        string? pathOverride,
        uint team,
        string? tomlParent,
        List<string> missingValues
    )
    {
        string? loadoutTomlPath =
            pathOverride
            ?? CombinePaths(
                tomlParent,
                ParseString(playerTable, "loadout_file", missingValues)
            );

        if (loadoutTomlPath == null)
            return null;

        TomlTable loadoutToml = GetTable(loadoutTomlPath);

        string teamLoadoutString = team == 0 ? "blue_loadout" : "orange_loadout";
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
        string? nameOverride = ParseString(rlbotPlayerTable, "name", missingValues);
        string? loadoutPathOverride = ParseString(
            rlbotPlayerTable,
            "loadout_file",
            missingValues
        );

        string? matchConfigParent = Path.GetDirectoryName(matchConfigPath);

        string? playerTomlPath = CombinePaths(
            matchConfigParent,
            ParseString(rlbotPlayerTable, "config", missingValues)
        );
        TomlTable playerToml = GetTable(playerTomlPath);
        string? tomlParent = Path.GetDirectoryName(playerTomlPath);

        TomlTable playerSettings = ParseTable(playerToml, "settings", missingValues);

        var name =
            nameOverride
            ?? ParseString(playerSettings, "name", missingValues)
            ?? "Unnamed RLBot";
        var agentId =
            ParseString(playerSettings, "agent_id", missingValues) ?? $"rlbot/{name}";
        uint team = ParseUint(rlbotPlayerTable, "team", 0, missingValues);

        return new PlayerConfigurationT
        {
            Variety = classUnion,
            Team = team,
            Name = name,
            RootDir = CombinePaths(
                tomlParent,
                ParseString(playerSettings, "root_dir", missingValues) ?? ""
            ),
            RunCommand = GetRunCommand(playerSettings, missingValues),
            Loadout = GetPlayerLoadout(
                playerSettings,
                loadoutPathOverride,
                team,
                tomlParent,
                missingValues
            ),
            Hivemind = ParseBool(playerSettings, "hivemind", false, missingValues),
            AgentId = agentId,
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
                MatchLengthMutator.FiveMinutes,
                missingValues
            ),
            MaxScore = ParseEnum(
                mutatorTable,
                "max_score",
                MaxScoreMutator.Default,
                missingValues
            ),
            MultiBall = ParseEnum(
                mutatorTable,
                "multi_ball",
                MultiBallMutator.One,
                missingValues
            ),
            Overtime = ParseEnum(
                mutatorTable,
                "overtime",
                OvertimeMutator.Unlimited,
                missingValues
            ),
            GameSpeed = ParseEnum(
                mutatorTable,
                "game_speed",
                GameSpeedMutator.Default,
                missingValues
            ),
            BallMaxSpeed = ParseEnum(
                mutatorTable,
                "ball_max_speed",
                BallMaxSpeedMutator.Default,
                missingValues
            ),
            BallType = ParseEnum(
                mutatorTable,
                "ball_type",
                BallTypeMutator.Default,
                missingValues
            ),
            BallWeight = ParseEnum(
                mutatorTable,
                "ball_weight",
                BallWeightMutator.Default,
                missingValues
            ),
            BallSize = ParseEnum(
                mutatorTable,
                "ball_size",
                BallSizeMutator.Default,
                missingValues
            ),
            BallBounciness = ParseEnum(
                mutatorTable,
                "ball_bounciness",
                BallBouncinessMutator.Default,
                missingValues
            ),
            Boost = ParseEnum(
                mutatorTable,
                "boost_amount",
                BoostMutator.NormalBoost,
                missingValues
            ),
            Rumble = ParseEnum(mutatorTable, "rumble", RumbleMutator.NoRumble, missingValues),
            BoostStrength = ParseEnum(
                mutatorTable,
                "boost_strength",
                BoostStrengthMutator.One,
                missingValues
            ),
            Gravity = ParseEnum(
                mutatorTable,
                "gravity",
                GravityMutator.Default,
                missingValues
            ),
            Demolish = ParseEnum(
                mutatorTable,
                "demolish",
                DemolishMutator.Default,
                missingValues
            ),
            RespawnTime = ParseEnum(
                mutatorTable,
                "respawn_time",
                RespawnTimeMutator.ThreeSeconds,
                missingValues
            ),
            MaxTime = ParseEnum(
                mutatorTable,
                "max_time",
                MaxTimeMutator.Default,
                missingValues
            ),
            GameEvent = ParseEnum(
                mutatorTable,
                "game_event",
                GameEventMutator.Default,
                missingValues
            ),
            Audio = ParseEnum(mutatorTable, "audio", AudioMutator.Default, missingValues),
        };

    public static MatchConfigurationT GetMatchConfig(string path)
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

        PsyonixLoadouts.Reset();
        List<PlayerConfigurationT> playerConfigs = [];
        foreach (var player in players)
            playerConfigs.Add(GetPlayerConfig(player, path, missingValues["cars"]));

        List<ScriptConfigurationT> scriptConfigs = [];
        foreach (var script in scripts)
            scriptConfigs.Add(GetScriptConfig(script, path, missingValues["scripts"]));

        var matchConfig = new MatchConfigurationT
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
            LauncherArg =
                ParseString(rlbotTable, "launcher_arg", missingValues["rlbot"]) ?? "",
            GameMode = ParseEnum(
                matchTable,
                "game_mode",
                GameMode.Soccer,
                missingValues["match"]
            ),
            GameMapUpk =
                ParseString(matchTable, "game_map_upk", missingValues["match"]) ?? "Stadium_P",
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
            Mutators = GetMutatorSettings(mutatorTable, missingValues["mutators"]),
            PlayerConfigurations = playerConfigs,
            ScriptConfigurations = scriptConfigs,
        };

        if (missingValues.Count > 0)
        {
            string missingValuesString = string.Join(
                ", ",
                missingValues.SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}.{v}"))
            );
            Logger.LogDebug($"Missing values in toml: {missingValuesString}");
        }

        return matchConfig;
    }
}
