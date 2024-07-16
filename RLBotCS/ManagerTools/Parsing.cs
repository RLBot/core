using System.Runtime.InteropServices;
using rlbot.flat;
using RLBotCS.Conversion;
using Tomlyn;
using Tomlyn.Model;

namespace RLBotCS.ManagerTools;

public static class ConfigParser
{
    private static TomlTable GetTable(string? path)
    {
        if (path == null)
        {
            Console.WriteLine("Warning! Could not read Toml file, path is null");
            return [];
        }

        try
        {
            // TODO - catch any exceptions thrown by ToModel
            return Toml.ToModel(File.ReadAllText(path));
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Warning! Could not read Toml file at '{path}'");
            return [];
        }
    }

    // GetTable retrieves a TomlTable from a file. ParseTable retrieves a table within another table

    private static TomlTable ParseTable(TomlTable table, string key)
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

    private static TomlTableArray ParseTableArray(TomlTable table, string key)
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

    // Get the enum value of a given enum and the string name of the desired key
    private static T ParseEnum<T>(TomlTable table, string key, T fallback)
        where T : struct, Enum
    {
        try
        {
            if (Enum.TryParse((string)table[key], true, out T value))
                return value;

            Console.WriteLine($"Warning! Unable to read '{key}', using default setting instead");
            return fallback;
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine($"Warning! Could not find the '{key}' field in toml. Using default setting instead");
            return fallback;
        }
    }

    private static int ParseInt(TomlTable table, string key, int fallback)
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

    private static uint ParseUint(TomlTable table, string key, uint fallback)
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

    private static float ParseFloat(TomlTable table, string key, float fallback)
    {
        try
        {
            return Convert.ToSingle(table[key]);
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine(
                $"Could not find the '{key}' field in toml. Using default setting '{fallback}' instead"
            );
            return fallback;
        }
    }

    private static string? ParseString(TomlTable table, string key, string? fallback)
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

    private static string? CombinePaths(string? parent, string? child)
    {
        if (parent == null || child == null)
            return null;

        return Path.Combine(parent, child);
    }

    private static bool ParseBool(TomlTable table, string key, bool fallback)
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

    private static string GetRunCommand(TomlTable runnableSettings)
    {
        string? runCommandWindows = ParseString(runnableSettings, "run_command", null);
        string? runCommandLinux = ParseString(runnableSettings, "run_command_linux", null);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return runCommandWindows ?? "";

        if (runCommandLinux != null)
            return runCommandLinux;

        // TODO:
        // We're currently on Linux but there's no Linux-specific run command
        // Try running the Windows command under Wine instead
        Console.WriteLine("Warning! No Linux-specific run command found for script!");
        return runCommandWindows ?? "";
    }

    private static ScriptConfigurationT GetScriptConfig(TomlTable scriptTable, string playerTomlPath)
    {
        string? scriptTomlPath = ParseString(scriptTable, "config", null);
        TomlTable scriptToml = GetTable(CombinePaths(playerTomlPath, scriptTomlPath));
        string tomlParent = Path.GetDirectoryName(scriptTomlPath) ?? "";

        ScriptConfigurationT scriptConfig =
            new()
            {
                Location = CombinePaths(tomlParent, ParseString(scriptToml, "location", "")),
                RunCommand = GetRunCommand(scriptToml)
            };
        return scriptConfig;
    }

    private static PlayerConfigurationT GetPlayerConfig(TomlTable table, string matchConfigPath) =>
        ParseEnum(table, "type", PlayerClass.RLBot) switch
        {
            PlayerClass.RLBot => GetBotConfig(table, PlayerClassUnion.FromRLBot(new RLBotT()), matchConfigPath),
            PlayerClass.Human => GetHumanConfig(table, PlayerClassUnion.FromHuman(new HumanT())),
            PlayerClass.Psyonix
                => GetPsyonixConfig(
                    table,
                    PlayerClassUnion.FromPsyonix(new PsyonixT { BotSkill = ParseFloat(table, "skill", 1.0f) })
                ),
            PlayerClass.PartyMember => throw new NotImplementedException("PartyMember not implemented"),
            _ => throw new NotImplementedException("Unimplemented PlayerClass type")
        };

    private static PlayerConfigurationT GetHumanConfig(TomlTable table, PlayerClassUnion classUnion) =>
        new()
        {
            Variety = classUnion,
            Team = ParseUint(table, "team", 0),
            Name = "Human",
            Location = "",
            RunCommand = ""
        };

    private static PlayerConfigurationT GetPsyonixConfig(TomlTable table, PlayerClassUnion classUnion)
    {
        var team = ParseUint(table, "team", 0);
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

    private static PlayerConfigurationT GetBotConfig(
        TomlTable rlbotPlayerTable,
        PlayerClassUnion classUnion,
        string matchConfigPath
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

        string? playerTomlPath = CombinePaths(matchConfigParent, ParseString(rlbotPlayerTable, "config", null));
        TomlTable playerToml = GetTable(CombinePaths(matchConfigParent, playerTomlPath));
        string? tomlParent = Path.GetDirectoryName(playerTomlPath);

        TomlTable playerSettings = ParseTable(playerToml, "settings");
        string? loadoutTomlPath = CombinePaths(tomlParent, ParseString(playerSettings, "looks_config", null));

        TomlTable loadoutToml = GetTable(loadoutTomlPath);

        string teamLoadoutString = ParseInt(rlbotPlayerTable, "team", 0) == 0 ? "blue_loadout" : "orange_loadout";
        TomlTable teamLoadout = ParseTable(loadoutToml, teamLoadoutString);

        TomlTable teamPaint = ParseTable(teamLoadout, "paint");

        return new PlayerConfigurationT
        {
            Variety = classUnion,
            Team = ParseUint(rlbotPlayerTable, "team", 0),
            Name = ParseString(playerSettings, "name", "Unnamed RLBot"),
            Location = CombinePaths(tomlParent, ParseString(playerSettings, "location", "")),
            RunCommand = GetRunCommand(playerSettings),
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
        // Gets the PlayerConfigT object for the number of players requested
        int numBots = ParseInt(matchTable, "num_cars", 0);
        for (int i = 0; i < Math.Min(numBots, players.Count); i++)
            playerConfigs.Add(GetPlayerConfig(players[i], path));

        List<ScriptConfigurationT> scriptConfigs = [];
        int numScripts = ParseInt(matchTable, "num_scripts", 0);
        for (int i = 0; i < Math.Min(numScripts, scripts.Count); i++)
            scriptConfigs.Add(GetScriptConfig(scripts[i], path));

        return new MatchSettingsT
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
            Freeplay = ParseBool(matchTable, "freeplay", false),
            MutatorSettings = new MutatorSettingsT()
            {
                MatchLength = ParseEnum(mutatorTable, "match_length", MatchLength.Five_Minutes),
                MaxScore = ParseEnum(mutatorTable, "max_score", MaxScore.Default),
                MultiBall = ParseEnum(mutatorTable, "multi_ball", MultiBall.One),
                OvertimeOption = ParseEnum(mutatorTable, "overtime", OvertimeOption.Unlimited),
                GameSpeedOption = ParseEnum(mutatorTable, "game_speed", GameSpeedOption.Default),
                BallMaxSpeedOption = ParseEnum(mutatorTable, "ball_max_speed", BallMaxSpeedOption.Default),
                BallTypeOption = ParseEnum(mutatorTable, "ball_type", BallTypeOption.Default),
                BallWeightOption = ParseEnum(mutatorTable, "ball_weight", BallWeightOption.Default),
                BallSizeOption = ParseEnum(mutatorTable, "ball_size", BallSizeOption.Default),
                BallBouncinessOption = ParseEnum(mutatorTable, "ball_bounciness", BallBouncinessOption.Default),
                BoostOption = ParseEnum(mutatorTable, "boost_amount", BoostOption.Normal_Boost),
                RumbleOption = ParseEnum(mutatorTable, "rumble", RumbleOption.No_Rumble),
                BoostStrengthOption = ParseEnum(mutatorTable, "boost_strength", BoostStrengthOption.One),
                GravityOption = ParseEnum(mutatorTable, "gravity", GravityOption.Default),
                DemolishOption = ParseEnum(mutatorTable, "demolish", DemolishOption.Default),
                RespawnTimeOption = ParseEnum(mutatorTable, "respawn_time", RespawnTimeOption.Three_Seconds),
            },
            PlayerConfigurations = playerConfigs,
            ScriptConfigurations = scriptConfigs
        };
    }
}
