using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.Conversion;
using Tomlyn;
using Tomlyn.Model;

namespace RLBotCS.ManagerTools;

public static class ConfigParser
{
    public class ConfigParserException(string? message, Exception? innerException)
        : Exception(message, innerException);

    public static readonly ILogger Logger = Logging.GetLogger("ConfigParser");

    private static TomlTable LoadTable(string path)
    {
        FileAttributes attr = File.GetAttributes(path);
        if (attr.HasFlag(FileAttributes.Directory))
        {
            throw new ArgumentException(
                "The specified path is a directory, not a config file: " + path
            );
        }
        path = Path.GetFullPath(path);
        return Toml.ToModel(File.ReadAllText(path), path);
    }

    private static T GetValue<T>(
        this TomlTable table,
        string key,
        T fallback,
        List<string> missingValues
    )
    {
        try
        {
            if (table.TryGetValue(key, out var res))
                return (T)res;
            missingValues.Add(key);
            return fallback;
        }
        catch (InvalidCastException e)
        {
            var v = table[key];
            if (v is string s)
                v = $"\"{s}\"";
            throw new InvalidCastException(
                $"Field '{key}' has value {v}, but a value of type {typeof(T).Name} was expected.",
                e
            );
        }
    }

    private static T GetEnum<T>(
        this TomlTable table,
        string key,
        T fallback,
        List<string> missingValues
    )
        where T : struct, Enum
    {
        if (table.TryGetValue(key, out var val))
        {
            if (Enum.TryParse((string)val, true, out T res))
                return res;
            throw new InvalidCastException(
                $"{val} is not a valid value for field '{key}'. Find valid values on https:/wiki.rlbot.org."
            );
        }

        missingValues.Add(key);
        return fallback;
    }

    private static string? CombinePaths(string? parent, string? child)
    {
        if (parent == null || child == null)
            return null;

        return Path.Combine(parent, child);
    }

    private static string GetRunCommand(TomlTable runnableSettings, List<string> missingValues)
    {
        string runCommandWindows = runnableSettings.GetValue<string>(
            "run_command",
            "",
            missingValues
        );

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return runCommandWindows;

        return runnableSettings.GetValue(
            "run_command_linux",
            runCommandWindows,
            missingValues
        );
    }

    private static ScriptConfigurationT LoadScriptConfig(
        string scriptConfigPath,
        List<string> missingValues
    )
    {
        TomlTable scriptToml = LoadTable(scriptConfigPath);
        string tomlParent = Path.GetDirectoryName(scriptConfigPath) ?? "";

        TomlTable settings = scriptToml.GetValue<TomlTable>("settings", [], missingValues);

        string name = settings.GetValue("name", "", missingValues);
        string agentId = settings.GetValue("agent_id", "", missingValues);

        ScriptConfigurationT scriptConfig = new()
        {
            Name = name,
            RootDir = CombinePaths(
                tomlParent,
                settings.GetValue("root_dir", "", missingValues)
            ),
            RunCommand = GetRunCommand(settings, missingValues),
            AgentId = agentId,
        };
        return scriptConfig;
    }

    private static uint GetTeam(TomlTable table, List<string> missingValues)
    {
        if (!table.TryGetValue("team", out var raw))
        {
            missingValues.Add("team");
            return 0;
        }

        switch (raw)
        {
            case long i
            and >= 0
            and <= 1: // Toml numbers are longs by default
                return (uint)i;
            case string s when s.Equals("blue", StringComparison.OrdinalIgnoreCase):
                return 0;
            case string s when s.Equals("orange", StringComparison.OrdinalIgnoreCase):
                return 1;
            default:
                throw new InvalidCastException(
                    $"{raw} is not a valid value for field 'team'. Use 0, 1, \"blue\", or \"orange\"."
                );
        }
    }

    private static PlayerConfigurationT ParseCarTable(
        TomlTable table,
        string matchConfigPath,
        List<string> missingValues
    )
    {
        var matchConfigDir = Path.GetDirectoryName(matchConfigPath)!;

        uint team = GetTeam(table, missingValues);
        string? nameOverride = table.GetValue<string?>("name", null, missingValues);
        string? loadoutFileOverride = table.GetValue<string?>(
            "loadout_file",
            null,
            missingValues
        );
        if (!string.IsNullOrEmpty(loadoutFileOverride))
            loadoutFileOverride = Path.Combine(matchConfigDir, loadoutFileOverride);

        PlayerClass playerClass = table.GetEnum("type", PlayerClass.CustomBot, missingValues);

        (PlayerClassUnion variety, bool useConfig) = playerClass switch
        {
            PlayerClass.CustomBot => (PlayerClassUnion.FromCustomBot(new CustomBotT()), true),
            PlayerClass.Psyonix => (
                PlayerClassUnion.FromPsyonix(
                    new PsyonixT
                    {
                        BotSkill = table.GetEnum("skill", PsyonixSkill.AllStar, missingValues),
                    }
                ),
                true
            ),
            PlayerClass.Human => (PlayerClassUnion.FromHuman(new HumanT()), false),
            PlayerClass.PartyMember => throw new NotImplementedException(
                "PartyMember not implemented"
            ),
        };

        string configPath = useConfig ? table.GetValue("config", "", missingValues) : "";

        PlayerConfigurationT player;
        if (useConfig && configPath == "" && variety.Type == PlayerClass.CustomBot)
        {
            throw new FileNotFoundException(
                $"Found a car with type 'rlbot' with empty 'config' field in {matchConfigPath}. "
                    + $"RLBot bots must specify a config file."
            );
        }

        if (useConfig && configPath != "")
        {
            string absoluteConfigPath = Path.Combine(matchConfigDir, configPath);
            player = LoadPlayerConfig(
                absoluteConfigPath,
                variety,
                team,
                nameOverride,
                loadoutFileOverride,
                missingValues
            );
        }
        else
        {
            PlayerLoadoutT? loadout = null;
            if (loadoutFileOverride is not null)
            {
                loadout = LoadPlayerLoadout(loadoutFileOverride, team, missingValues);
            }
            player = new PlayerConfigurationT
            {
                AgentId = "",
                Variety = variety,
                Name = nameOverride,
                Team = team,
                Loadout = loadout,
                Hivemind = false,
                RootDir = "",
                RunCommand = "",
                SpawnId = 0,
            };
        }

        return player;
    }

    private static PlayerConfigurationT LoadPlayerConfig(
        string configPath,
        PlayerClassUnion variety,
        uint team,
        string? nameOverride,
        string? loadoutFileOverride,
        List<string> missingValues
    )
    {
        TomlTable table = LoadTable(configPath);

        TomlTable settings = table.GetValue<TomlTable>("settings", [], missingValues);
        string configDir = Path.GetDirectoryName(configPath)!;
        string rootDir = Path.Combine(
            configDir,
            settings.GetValue<string>("root_dir", "", missingValues)
        );

        // Override is null, "", or an absolute path.
        // Null implies no override and "" implies we should not load the loadout.
        string? loadoutPath = loadoutFileOverride;
        if (
            loadoutFileOverride is null
            && settings.TryGetValue("loadout_file", out var loadoutPathRel)
        )
        {
            loadoutPath = Path.Combine(configDir, (string)loadoutPathRel);
        }

        PlayerLoadoutT? loadout =
            (loadoutPath ?? "") != ""
                ? LoadPlayerLoadout(loadoutPath, team, missingValues)
                : null;

        return new PlayerConfigurationT
        {
            AgentId = settings.GetValue<string>("agent_id", "", missingValues),
            Name = nameOverride ?? settings.GetValue<string>("name", "", missingValues),
            Team = team,
            Loadout = loadout,
            RunCommand = GetRunCommand(settings, missingValues),
            Hivemind = settings.GetValue("hivemind", false, missingValues),
            RootDir = rootDir,
            SpawnId = 0,
            Variety = variety,
        };
    }

    private static PlayerLoadoutT LoadPlayerLoadout(
        string loadoutPath,
        uint team,
        List<string> missingValues
    )
    {
        TomlTable loadoutToml = LoadTable(loadoutPath);

        string teamLoadoutString = team == 0 ? "blue_loadout" : "orange_loadout";
        TomlTable teamLoadout = loadoutToml.GetValue<TomlTable>(
            teamLoadoutString,
            [],
            missingValues
        );
        TomlTable teamPaint = teamLoadout.GetValue<TomlTable>("paint", [], missingValues);

        return new PlayerLoadoutT()
        {
            TeamColorId = (uint)teamLoadout.GetValue<long>("team_color_id", 0, missingValues),
            CustomColorId = (uint)
                teamLoadout.GetValue<long>("custom_color_id", 0, missingValues),
            CarId = (uint)teamLoadout.GetValue<long>("car_id", 0, missingValues),
            DecalId = (uint)teamLoadout.GetValue<long>("decal_id", 0, missingValues),
            WheelsId = (uint)teamLoadout.GetValue<long>("wheels_id", 0, missingValues),
            BoostId = (uint)teamLoadout.GetValue<long>("boost_id", 0, missingValues),
            AntennaId = (uint)teamLoadout.GetValue<long>("antenna_id", 0, missingValues),
            HatId = (uint)teamLoadout.GetValue<long>("hat_id", 0, missingValues),
            PaintFinishId = (uint)
                teamLoadout.GetValue<long>("paint_finish_id", 0, missingValues),
            CustomFinishId = (uint)
                teamLoadout.GetValue<long>("custom_finish_id", 0, missingValues),
            EngineAudioId = (uint)
                teamLoadout.GetValue<long>("engine_audio_id", 0, missingValues),
            TrailsId = (uint)teamLoadout.GetValue<long>("trails_id", 0, missingValues),
            GoalExplosionId = (uint)
                teamLoadout.GetValue<long>("goal_explosion_id", 0, missingValues),
            LoadoutPaint = new LoadoutPaintT
            {
                CarPaintId = (uint)teamPaint.GetValue<long>("car_paint_id", 0, missingValues),
                DecalPaintId = (uint)
                    teamPaint.GetValue<long>("decal_paint_id", 0, missingValues),
                WheelsPaintId = (uint)
                    teamPaint.GetValue<long>("wheels_paint_id", 0, missingValues),
                BoostPaintId = (uint)
                    teamPaint.GetValue<long>("boost_paint_id", 0, missingValues),
                AntennaPaintId = (uint)
                    teamPaint.GetValue<long>("antenna_paint_id", 0, missingValues),
                HatPaintId = (uint)teamPaint.GetValue<long>("hat_paint_id", 0, missingValues),
                TrailsPaintId = (uint)
                    teamPaint.GetValue<long>("trails_paint_id", 0, missingValues),
                GoalExplosionPaintId = (uint)
                    teamPaint.GetValue<long>("goal_explosion_paint_id", 0, missingValues),
            },
            // TODO - GetPrimary/Secondary color? Do any bots use this?
        };
    }

    private static MutatorSettingsT GetMutatorSettings(
        TomlTable mutatorTable,
        List<string> missingValues
    ) =>
        new MutatorSettingsT
        {
            MatchLength = mutatorTable.GetEnum(
                "match_length",
                MatchLengthMutator.FiveMinutes,
                missingValues
            ),
            MaxScore = mutatorTable.GetEnum(
                "max_score",
                MaxScoreMutator.Default,
                missingValues
            ),
            MultiBall = mutatorTable.GetEnum(
                "multi_ball",
                MultiBallMutator.One,
                missingValues
            ),
            Overtime = mutatorTable.GetEnum(
                "overtime",
                OvertimeMutator.Unlimited,
                missingValues
            ),
            GameSpeed = mutatorTable.GetEnum(
                "game_speed",
                GameSpeedMutator.Default,
                missingValues
            ),
            BallMaxSpeed = mutatorTable.GetEnum(
                "ball_max_speed",
                BallMaxSpeedMutator.Default,
                missingValues
            ),
            BallType = mutatorTable.GetEnum(
                "ball_type",
                BallTypeMutator.Default,
                missingValues
            ),
            BallWeight = mutatorTable.GetEnum(
                "ball_weight",
                BallWeightMutator.Default,
                missingValues
            ),
            BallSize = mutatorTable.GetEnum(
                "ball_size",
                BallSizeMutator.Default,
                missingValues
            ),
            BallBounciness = mutatorTable.GetEnum(
                "ball_bounciness",
                BallBouncinessMutator.Default,
                missingValues
            ),
            Boost = mutatorTable.GetEnum(
                "boost_amount",
                BoostMutator.NormalBoost,
                missingValues
            ),
            Rumble = mutatorTable.GetEnum("rumble", RumbleMutator.NoRumble, missingValues),
            BoostStrength = mutatorTable.GetEnum(
                "boost_strength",
                BoostStrengthMutator.One,
                missingValues
            ),
            Gravity = mutatorTable.GetEnum("gravity", GravityMutator.Default, missingValues),
            Demolish = mutatorTable.GetEnum(
                "demolish",
                DemolishMutator.Default,
                missingValues
            ),
            RespawnTime = mutatorTable.GetEnum(
                "respawn_time",
                RespawnTimeMutator.ThreeSeconds,
                missingValues
            ),
            MaxTime = mutatorTable.GetEnum("max_time", MaxTimeMutator.Default, missingValues),
            GameEvent = mutatorTable.GetEnum(
                "game_event",
                GameEventMutator.Default,
                missingValues
            ),
            Audio = mutatorTable.GetEnum("audio", AudioMutator.Default, missingValues),
        };

    /// <summary>
    /// Loads the match configuration at the given path. Empty fields are given default values.
    /// However, default values are not necessarily valid (e.g. empty agent_id).
    /// Use <see cref="ConfigValidator"/> to validate the match config.
    /// </summary>
    /// <param name="path">Path to match configuration file.</param>
    /// <returns>Whether the match config was successfully loaded. Potential errors are logged.</returns>
    public static bool TryLoadMatchConfig(string path, out MatchConfigurationT config)
    {
        config = null;
        try
        {
            config = LoadMatchConfig(path);
            return true;
        }
        catch (ConfigParserException e)
        {
            Logger.LogError(e.Message);
        }
        return false;
    }

    /// <summary>
    /// Loads the match configuration at the given path. Empty fields are given default values.
    /// However, default values are not necessarily valid (e.g. empty agent_id).
    /// Use <see cref="ConfigValidator"/> to validate the match config.
    /// </summary>
    /// <param name="path">Path to match configuration file.</param>
    /// <returns>The parsed MatchConfigurationT</returns>
    /// <exception cref="ConfigParserException">Thrown if something went wrong. See inner exception.</exception>
    public static MatchConfigurationT LoadMatchConfig(string path)
    {
        try
        {
            path = Path.GetFullPath(path);
            TomlTable rlbotToml = LoadTable(path);

            List<string> missingValues = [];

            TomlTable rlbotTable = rlbotToml.GetValue<TomlTable>("rlbot", [], missingValues);
            TomlTable matchTable = rlbotToml.GetValue<TomlTable>("match", [], missingValues);
            TomlTable mutatorTable = rlbotToml.GetValue<TomlTable>(
                "mutators",
                [],
                missingValues
            );

            TomlTableArray players = rlbotToml.GetValue<TomlTableArray>(
                "cars",
                [],
                missingValues
            );
            List<PlayerConfigurationT> playerConfigs = [];
            foreach (var playerTable in players)
                playerConfigs.Add(ParseCarTable(playerTable, path, missingValues));

            TomlTableArray scripts = rlbotToml.GetValue<TomlTableArray>(
                "scripts",
                [],
                missingValues
            );
            List<ScriptConfigurationT> scriptConfigs = [];
            foreach (var scriptTable in scripts)
            {
                string configPath = scriptTable.GetValue("config", "", missingValues);
                if (configPath != "")
                {
                    string absoluteConfigPath = Path.Combine(
                        Path.GetDirectoryName(path),
                        configPath
                    );
                    scriptConfigs.Add(LoadScriptConfig(absoluteConfigPath, missingValues));
                }
                else
                {
                    throw new FileNotFoundException(
                        $"Found a script with empty 'config' field in {path}. "
                            + $"Scripts must specify a config file."
                    );
                }
            }

            var matchConfig = new MatchConfigurationT
            {
                Launcher = rlbotTable.GetEnum("launcher", Launcher.Steam, missingValues),
                AutoStartBots = rlbotTable.GetValue("auto_start_bots", true, missingValues),
                LauncherArg = rlbotTable.GetValue("launcher_arg", "", missingValues),
                GameMode = matchTable.GetEnum("game_mode", GameMode.Soccer, missingValues),
                GameMapUpk = matchTable.GetValue("game_map_upk", "Stadium_P", missingValues),
                SkipReplays = matchTable.GetValue("skip_replays", false, missingValues),
                InstantStart = matchTable.GetValue(
                    "start_without_countdown",
                    false,
                    missingValues
                ),
                EnableRendering = matchTable.GetValue(
                    "enable_rendering",
                    false,
                    missingValues
                ),
                EnableStateSetting = matchTable.GetValue(
                    "enable_state_setting",
                    true,
                    missingValues
                ),
                ExistingMatchBehavior = matchTable.GetEnum(
                    "existing_match_behavior",
                    ExistingMatchBehavior.Restart,
                    missingValues
                ),
                AutoSaveReplay = matchTable.GetValue("auto_save_replay", false, missingValues),
                Freeplay = matchTable.GetValue("freeplay", false, missingValues),
                Mutators = GetMutatorSettings(mutatorTable, missingValues),
                PlayerConfigurations = playerConfigs,
                ScriptConfigurations = scriptConfigs,
            };

            // TODO: Report missing values again

            return matchConfig;
        }
        catch (Exception e)
        {
            throw new ConfigParserException(
                "Failed to load match config. " + e.Message.Trim(),
                e
            );
        }
    }
}
