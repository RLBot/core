using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using Tomlyn;
using Tomlyn.Model;

namespace RLBotCS.ManagerTools;

public class ConfigParser
{
    public class ConfigParserException(string? message, Exception? innerException = null)
        : Exception(message, innerException);

    private readonly ILogger Logger = Logging.GetLogger("ConfigParser");

    /// <summary>Used to provide accurate error messages.</summary>
    private readonly ConfigContextTracker _context = new();
    
    /// <summary>Holds field names that were not present in the config. Used for debugging.</summary>
    private readonly List<string> _missingValues = new();

    private TomlTable LoadTomlFile(string path)
    {
        try
        {
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                throw new ArgumentException(
                    $"The specified path is a directory, not a config file ({path})"
                );
            }

            path = Path.GetFullPath(path);
            return Toml.ToModel(File.ReadAllText(path), path);
        }
        catch (Exception e)
        {
            string ctx = _context.IsEmpty ? "" : $"{_context}: ";
            throw new ConfigParserException($"{ctx}" + e.Message, e);
        }
    }

    private T GetValue<T>(
        TomlTable table,
        string key,
        T fallback
    )
    {
        try
        {
            if (table.TryGetValue(key, out var res))
                return (T)res;
            _missingValues.Add(_context.ToStringWithEnd(key));
            return fallback;
        }
        catch (InvalidCastException e)
        {
            var v = table[key];
            if (v is string s)
                v = $"\"{s}\"";
            throw new InvalidCastException(
                $"{_context.ToStringWithEnd(key)} has value {v}, but a value of type {typeof(T).Name} was expected.",
                e
            );
        }
    }

    private T GetEnum<T>(
        TomlTable table,
        string key,
        T fallback
    )
        where T : struct, Enum
    {
        if (table.TryGetValue(key, out var raw))
        {
            if (raw is string val)
            {
                if (Enum.TryParse((string)val, true, out T res))
                    return res;
                throw new InvalidCastException(
                    $"{_context.ToStringWithEnd(key)} has invalid value \"{raw}\". " +
                    $"Find valid values on https:/wiki.rlbot.org."
                );
            }
            else
            {
                throw new InvalidCastException($"{_context.ToStringWithEnd(key)} has value {raw}, but a value of type {typeof(T).Name} was expected.");
            }
        }

        _missingValues.Add(_context.ToStringWithEnd(key));
        return fallback;
    }

    private static string? CombinePaths(string? parent, string? child)
    {
        if (parent == null || child == null)
            return null;

        return Path.Combine(parent, child);
    }

    private string GetRunCommand(TomlTable runnableSettings)
    {
        string runCommandWindows = GetValue<string>(runnableSettings, "run_command", "");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return runCommandWindows;

        return GetValue(runnableSettings, "run_command_linux", runCommandWindows);
    }

    private ScriptConfigurationT LoadScriptConfig(string scriptConfigPath)
    {
        TomlTable scriptToml = LoadTomlFile(scriptConfigPath);
        string tomlParent = Path.GetDirectoryName(scriptConfigPath) ?? "";

        TomlTable settings = GetValue<TomlTable>(scriptToml, "settings", []);
        using (_context.Begin("settings"))
        {
            ScriptConfigurationT scriptConfig = new()
            {
                Name = GetValue(settings, "name", ""),
                RootDir = CombinePaths(tomlParent, GetValue(settings, "root_dir", "")),
                RunCommand = GetRunCommand(settings),
                AgentId = GetValue(settings, "agent_id", ""),
            };
            return scriptConfig;
        }
    }

    private uint GetTeam(TomlTable table, List<string> missingValues)
    {
        if (!table.TryGetValue("team", out var raw))
        {
            missingValues.Add(_context.ToStringWithEnd("team"));
            return 0;
        }

        switch (raw)
        {
            // Toml numbers are longs by default
            case long i
                and >= 0
                and <= 1:
                return (uint)i;
            case string s when s.Equals("blue", StringComparison.OrdinalIgnoreCase):
                return 0;
            case string s when s.Equals("orange", StringComparison.OrdinalIgnoreCase):
                return 1;
            default:
                if (raw is string str)
                    raw = $"\"{str}\"";
                throw new InvalidCastException(
                    $"{_context.ToStringWithEnd("team")} has invalid value {raw}. " +
                    $"Use 0, 1, \"blue\", or \"orange\"."
                );
        }
    }

    private PlayerConfigurationT ParseCarTable(TomlTable table, string matchConfigPath)
    {
        var matchConfigDir = Path.GetDirectoryName(matchConfigPath)!;

        uint team = GetTeam(table, _missingValues);
        string? nameOverride = GetValue<string?>(table, "name", null);
        string? loadoutFileOverride = GetValue<string?>(table, "loadout_file", null);
        if (!string.IsNullOrEmpty(loadoutFileOverride))
        {
            loadoutFileOverride = Path.Combine(matchConfigDir, loadoutFileOverride);
        }

        PlayerClass playerClass = GetEnum(table, "type", PlayerClass.CustomBot);

        (PlayerClassUnion variety, bool useConfig) = playerClass switch
        {
            PlayerClass.CustomBot => (PlayerClassUnion.FromCustomBot(new CustomBotT()), true),
            PlayerClass.Psyonix => (
                PlayerClassUnion.FromPsyonix(
                    new PsyonixT
                    {
                        BotSkill = GetEnum(table, "skill", PsyonixSkill.AllStar),
                    }
                ),
                true
            ),
            PlayerClass.Human => (PlayerClassUnion.FromHuman(new HumanT()), false),
            PlayerClass.PartyMember => throw new NotImplementedException(
                "PartyMember not implemented"
            ),
            _ => throw new ConfigParserException(
                $"{_context.ToStringWithEnd("type")} is out of range."
            ),
        };

        string configPath = useConfig ? GetValue(table, "config", "") : "";

        PlayerConfigurationT player;
        if (useConfig && configPath == "" && variety.Type == PlayerClass.CustomBot)
        {
            throw new FileNotFoundException(
                $"{_context} has type 'rlbot' but {_context.ToStringWithEnd("config")} is empty. "
                + $"RLBot bots must specify a config file."
            );
        }

        if (useConfig && configPath != "")
        {
            string absoluteConfigPath = Path.Combine(matchConfigDir, configPath);
            using (_context.Begin("config", ConfigContextTracker.Type.Link))
            {
                player = LoadPlayerConfig(
                    absoluteConfigPath,
                    variety,
                    team,
                    nameOverride,
                    loadoutFileOverride
                );
            }
        }
        else
        {
            PlayerLoadoutT? loadout = null;
            if (loadoutFileOverride is not null)
            {
                using (_context.Begin("loadout_file", ConfigContextTracker.Type.Link))
                {
                    loadout = LoadPlayerLoadout(loadoutFileOverride, team);
                }
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

    private PlayerConfigurationT LoadPlayerConfig(
        string configPath,
        PlayerClassUnion variety,
        uint team,
        string? nameOverride,
        string? loadoutFileOverride
    )
    {
        TomlTable table = LoadTomlFile(configPath);
        string configDir = Path.GetDirectoryName(configPath)!;

        TomlTable settings = GetValue<TomlTable>(table, "settings", []);
        using (_context.Begin("settings"))
        {

            string rootDir = Path.Combine(
                configDir,
                GetValue<string>(settings, "root_dir", "")
            );

            // Override is null, "", or an absolute path.
            // Null implies no override and "" implies we should not load the loadout.
            string? loadoutPath = loadoutFileOverride;
            if (loadoutFileOverride is null)
            {
                if (settings.TryGetValue("loadout_file", out var loadoutPathRel))
                {
                    loadoutPath = Path.Combine(configDir, (string)loadoutPathRel);
                }
                else
                {
                    _missingValues.Add(_context.ToStringWithEnd("loadout_file"));
                }
            }

            PlayerLoadoutT? loadout;
            using (_context.Begin("loadout_file", ConfigContextTracker.Type.Link))
            {
                 loadout = (loadoutPath ?? "") != ""
                        ? LoadPlayerLoadout(loadoutPath!, team)
                        : null;
            }

            return new PlayerConfigurationT
            {
                AgentId = GetValue<string>(settings, "agent_id", ""),
                Name = nameOverride ?? GetValue<string>(settings, "name", ""),
                Team = team,
                Loadout = loadout,
                RunCommand = GetRunCommand(settings),
                Hivemind = GetValue(settings, "hivemind", false),
                RootDir = rootDir,
                SpawnId = 0,
                Variety = variety,
            };
        }
    }

    private PlayerLoadoutT LoadPlayerLoadout(string loadoutPath, uint team)
    {
        TomlTable loadoutToml = LoadTomlFile(loadoutPath);

        string teamLoadoutString = team == 0 ? "blue_loadout" : "orange_loadout";
        TomlTable teamLoadout = GetValue<TomlTable>(loadoutToml,
            teamLoadoutString,
            []
        );
        using (_context.Begin(teamLoadoutString, ConfigContextTracker.Type.Link))
        {

            TomlTable teamPaint = GetValue<TomlTable>(teamLoadout, "paint", []);
            LoadoutPaintT loadoutPaint;
            using (_context.Begin("paint"))
            {
                loadoutPaint = new LoadoutPaintT
                {
                    // TODO - GetPrimary/Secondary color? Do any bots use this?
                    CarPaintId = (uint)GetValue<long>(teamPaint, "car_paint_id", 0),
                    DecalPaintId = (uint)GetValue<long>(teamPaint, "decal_paint_id", 0),
                    WheelsPaintId = (uint)GetValue<long>(teamPaint, "wheels_paint_id", 0),
                    BoostPaintId = (uint)GetValue<long>(teamPaint, "boost_paint_id", 0),
                    AntennaPaintId = (uint)GetValue<long>(teamPaint, "antenna_paint_id", 0),
                    HatPaintId = (uint)GetValue<long>(teamPaint, "hat_paint_id", 0),
                    TrailsPaintId = (uint)GetValue<long>(teamPaint, "trails_paint_id", 0),
                    GoalExplosionPaintId = (uint)GetValue<long>(teamPaint, "goal_explosion_paint_id", 0),
                };
            }

            return new PlayerLoadoutT()
            {
                TeamColorId = (uint)GetValue<long>(teamLoadout, "team_color_id", 0),
                CustomColorId = (uint)GetValue<long>(teamLoadout, "custom_color_id", 0),
                CarId = (uint)GetValue<long>(teamLoadout, "car_id", 0),
                DecalId = (uint)GetValue<long>(teamLoadout, "decal_id", 0),
                WheelsId = (uint)GetValue<long>(teamLoadout, "wheels_id", 0),
                BoostId = (uint)GetValue<long>(teamLoadout, "boost_id", 0),
                AntennaId = (uint)GetValue<long>(teamLoadout, "antenna_id", 0),
                HatId = (uint)GetValue<long>(teamLoadout, "hat_id", 0),
                PaintFinishId = (uint)GetValue<long>(teamLoadout, "paint_finish_id", 0),
                CustomFinishId = (uint)GetValue<long>(teamLoadout, "custom_finish_id", 0),
                EngineAudioId = (uint)GetValue<long>(teamLoadout, "engine_audio_id", 0),
                TrailsId = (uint)GetValue<long>(teamLoadout, "trails_id", 0),
                GoalExplosionId = (uint)GetValue<long>(teamLoadout, "goal_explosion_id", 0),
                LoadoutPaint = loadoutPaint,
            };
        }
    }

    private MutatorSettingsT GetMutatorSettings(TomlTable mutatorTable) => new MutatorSettingsT
        {
            MatchLength = GetEnum(mutatorTable,"match_length",MatchLengthMutator.FiveMinutes),
            MaxScore = GetEnum(mutatorTable,"max_score",MaxScoreMutator.Default),
            MultiBall = GetEnum(mutatorTable,"multi_ball",MultiBallMutator.One),
            Overtime = GetEnum(mutatorTable,"overtime",OvertimeMutator.Unlimited),
            GameSpeed = GetEnum(mutatorTable,"game_speed",GameSpeedMutator.Default),
            BallMaxSpeed = GetEnum(mutatorTable,"ball_max_speed",BallMaxSpeedMutator.Default),
            BallType = GetEnum(mutatorTable,"ball_type",BallTypeMutator.Default),
            BallWeight = GetEnum(mutatorTable,"ball_weight",BallWeightMutator.Default),
            BallSize = GetEnum(mutatorTable,"ball_size",BallSizeMutator.Default),
            BallBounciness = GetEnum(mutatorTable,"ball_bounciness",BallBouncinessMutator.Default),
            Boost = GetEnum(mutatorTable,"boost_amount",BoostMutator.NormalBoost),
            Rumble = GetEnum(mutatorTable, "rumble", RumbleMutator.NoRumble),
            BoostStrength = GetEnum(mutatorTable, "boost_strength", BoostStrengthMutator.One),
            Gravity = GetEnum(mutatorTable, "gravity", GravityMutator.Default),
            Demolish = GetEnum(mutatorTable, "demolish", DemolishMutator.Default),
            RespawnTime = GetEnum(mutatorTable, "respawn_time", RespawnTimeMutator.ThreeSeconds),
            MaxTime = GetEnum(mutatorTable, "max_time", MaxTimeMutator.Default),
            GameEvent = GetEnum(mutatorTable, "game_event", GameEventMutator.Default),
            Audio = GetEnum(mutatorTable, "audio", AudioMutator.Default),
        };

    /// <summary>
    /// Loads the match configuration at the given path. Empty fields are given default values.
    /// However, default values are not necessarily valid (e.g. empty agent_id).
    /// Use <see cref="ConfigValidator"/> to validate the match config.
    /// </summary>
    /// <param name="path">Path to match configuration file.</param>
    /// <param name="config">The loaded match config.</param>
    /// <returns>Whether the match config was successfully loaded. Potential errors are logged.</returns>
    public bool TryLoadMatchConfig(string path, out MatchConfigurationT config)
    {
        config = null!;
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
    public MatchConfigurationT LoadMatchConfig(string path)
    {
        _missingValues.Clear();
        _context.Clear();
        
        try
        {
            path = Path.GetFullPath(path);
            TomlTable outerTable = LoadTomlFile(path);

            MatchConfigurationT matchConfig = new MatchConfigurationT();

            TomlTable rlbotTable = GetValue<TomlTable>(outerTable, "rlbot", []);
            using (_context.Begin("rlbot"))
            {
                matchConfig.Launcher = GetEnum(rlbotTable, "launcher", Launcher.Steam);
                matchConfig.LauncherArg = GetValue(rlbotTable, "launcher_arg", "");
                matchConfig.AutoStartBots = GetValue(rlbotTable, "auto_start_bots", true);
            }
            
            TomlTableArray players = GetValue<TomlTableArray>(outerTable, "cars", []);
            matchConfig.PlayerConfigurations = [];
            for (var i = 0; i < players.Count; i++)
            {
                using (_context.Begin($"cars[{i}]"))
                {
                    matchConfig.PlayerConfigurations.Add(ParseCarTable(players[i], path));
                }
            }

            TomlTableArray scripts = GetValue<TomlTableArray>(outerTable, "scripts", []);
            matchConfig.ScriptConfigurations = [];
            for (var i = 0; i < scripts.Count; i++)
            {
                using (_context.Begin($"scripts[{i}]"))
                {
                    string configPath = GetValue(scripts[i], "config", "");
                    if (configPath != "")
                    {
                        string absoluteConfigPath = Path.Combine(
                            Path.GetDirectoryName(path)!,
                            configPath
                        );
                        using (_context.Begin("config", ConfigContextTracker.Type.Link))
                        {
                            matchConfig.ScriptConfigurations.Add(LoadScriptConfig(absoluteConfigPath));
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException(
                            $"{_context.ToStringWithEnd("config")} is empty. "
                            + $"Scripts must specify a config file."
                        );
                    }
                }
            }

            TomlTable mutatorTable = GetValue<TomlTable>(outerTable, "mutators", []);
            using (_context.Begin("mutators"))
            {
                matchConfig.Mutators = GetMutatorSettings(mutatorTable);
            }
            
            TomlTable matchTable = GetValue<TomlTable>(outerTable, "match", []);
            using (_context.Begin("match"))
            {
                matchConfig.GameMode = GetEnum(matchTable, "game_mode", GameMode.Soccer);
                matchConfig.GameMapUpk = GetValue(matchTable, "game_map_upk", "Stadium_P");
                matchConfig.SkipReplays = GetValue(matchTable, "skip_replays", false);
                matchConfig.InstantStart = GetValue(matchTable,"start_without_countdown",false);
                matchConfig.EnableRendering = GetValue(matchTable,"enable_rendering",false);
                matchConfig.EnableStateSetting = GetValue(matchTable,"enable_state_setting",true);
                matchConfig.ExistingMatchBehavior = GetEnum(matchTable,"existing_match_behavior",ExistingMatchBehavior.Restart);
                matchConfig.AutoSaveReplay = GetValue(matchTable, "auto_save_replay", false);
                matchConfig.Freeplay = GetValue(matchTable, "freeplay", false);
            }

            string mv = string.Join(",", _missingValues);
            Logger.LogDebug($"Missing values in toml: {mv}");
            
            Debug.Assert(_context.IsEmpty, $"Context not emptied: {_context}");
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
