using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBotCS.Model;
using Tomlyn;
using Tomlyn.Model;

namespace RLBotCS.ManagerTools;

public class ConfigParser
{
    public static class Fields
    {
        public const string RlBotTable = "rlbot";
        public const string RlBotLauncher = "launcher";
        public const string RlBotLauncherArg = "launcher_arg";
        public const string RlBotAutoStartAgents = "auto_start_agents";
        public const string RlBotAutoStartAgentsOld = "auto_start_bots";
        public const string RlBotWaitForAgents = "wait_for_agents";

        public const string MatchTable = "match";
        public const string MatchGameMode = "game_mode";
        public const string MatchMapUpk = "game_map_upk";
        public const string MatchSkipReplays = "skip_replays";
        public const string MatchStartWithoutCountdown = "start_without_countdown";
        public const string MatchExistingMatchBehavior = "existing_match_behavior";
        public const string MatchRendering = "enable_rendering";
        public const string MatchStateSetting = "enable_state_setting";
        public const string MatchAutoSaveReplays = "auto_save_replays";
        public const string MatchFreePlay = "freeplay";

        public const string MutatorsTable = "mutators";
        public const string MutatorsMatchLength = "match_length";
        public const string MutatorsMaxScore = "max_score";
        public const string MutatorsMultiBall = "multi_ball";
        public const string MutatorsOvertime = "overtime";
        public const string MutatorsGameSpeed = "game_speed";
        public const string MutatorsBallMaxSpeed = "ball_max_speed";
        public const string MutatorsBallType = "ball_type";
        public const string MutatorsBallWeight = "ball_weight";
        public const string MutatorsBallSize = "ball_size";
        public const string MutatorsBallBounciness = "ball_bounciness";
        public const string MutatorsBoostAmount = "boost_amount";
        public const string MutatorsRumble = "rumble";
        public const string MutatorsBoostStrength = "boost_strength";
        public const string MutatorsGravity = "gravity";
        public const string MutatorsDemolish = "demolish";
        public const string MutatorsRespawnTime = "respawn_time";
        public const string MutatorsMaxTime = "max_time";
        public const string MutatorsGameEvent = "game_event";
        public const string MutatorsAudio = "audio";
        public const string MutatorsBallGravity = "ball_gravity";
        public const string MutatorsTerritory = "territory";
        public const string MutatorsStaleBall = "stale_ball";
        public const string MutatorsJump = "jump";
        public const string MutatorsDodgeTimer = "dodge_timer";
        public const string MutatorsPossessionScore = "possession_score";
        public const string MutatorsDemolishScore = "demolish_score";
        public const string MutatorsNormalGoalScore = "normal_goal_score";
        public const string MutatorsAerialGoalScore = "aerial_goal_score";
        public const string MutatorsAssistGoalScore = "assist_goal_score";
        public const string MutatorsInputRestriction = "input_restriction";

        public const string CarsList = "cars";
        public const string ScriptsList = "scripts";
        public const string AgentTeam = "team";
        public const string AgentType = "type";
        public const string AgentSkill = "skill";
        public const string AgentAutoStart = "auto_start";
        public const string AgentName = "name";
        public const string AgentLoadoutFile = "loadout_file";
        public const string AgentConfigFile = "config_file";
        public const string AgentConfigFileOld = "config";
        public const string AgentSettingsTable = "settings";
        public const string AgentAgentId = "agent_id";
        public const string AgentRootDir = "root_dir";
        public const string AgentRunCommand = "run_command";
        public const string AgentRunCommandLinux = "run_command_linux";
        public const string AgentHivemind = "hivemind";

        public const string LoadoutBlueTable = "blue_loadout";
        public const string LoadoutOrangeTable = "orange_loadout";
        public const string LoadoutTeamColorId = "team_color_id";
        public const string LoadoutCustomColorId = "custom_color_id";
        public const string LoadoutCarId = "car_id";
        public const string LoadoutDecalId = "decal_id";
        public const string LoadoutWheelsId = "wheels_id";
        public const string LoadoutBoostId = "boost_id";
        public const string LoadoutAntennaId = "antenna_id";
        public const string LoadoutHatId = "hat_id";
        public const string LoadoutPaintFinishId = "paint_finish_id";
        public const string LoadoutCustomFinishId = "custom_finish_id";
        public const string LoadoutEngineAudioId = "engine_audio_id";
        public const string LoadoutTrailsId = "trails_id";
        public const string LoadoutGoalExplosionId = "goal_explosion_id";
        public const string LoadoutPaintTable = "paint";
        public const string LoadoutPaintCarPaintId = "car_paint_id";
        public const string LoadoutPaintDecalPaintId = "decal_paint_id";
        public const string LoadoutPaintWheelsPaintId = "wheels_paint_id";
        public const string LoadoutPaintBoostPaintId = "boost_paint_id";
        public const string LoadoutPaintAntennaPaintId = "antenna_paint_id";
        public const string LoadoutPaintHatPaintId = "hat_paint_id";
        public const string LoadoutPaintTrailsPaintId = "trails_paint_id";
        public const string LoadoutPaintGoalExplosionPaintId = "goal_explosion_paint_id";
    }

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

    private T GetValue<T>(TomlTable table, string key, T fallback)
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

    private T GetEnum<T>(TomlTable table, string key, T fallback)
        where T : struct, Enum
    {
        if (table.TryGetValue(key, out var raw))
        {
            if (raw is string val)
            {
                if (Enum.TryParse((string)val, true, out T res))
                    return res;
                throw new InvalidCastException(
                    $"{_context.ToStringWithEnd(key)} has invalid value \"{raw}\". "
                        + $"Find valid values on https:/wiki.rlbot.org."
                );
            }
            else
            {
                throw new InvalidCastException(
                    $"{_context.ToStringWithEnd(key)} has value {raw}, but a value of type {typeof(T).Name} was expected."
                );
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
        string runCommandWindows = GetValue<string>(
            runnableSettings,
            Fields.AgentRunCommand,
            ""
        );

#if WINDOWS
        return runCommandWindows;
#else
        return GetValue(runnableSettings, Fields.AgentRunCommandLinux, runCommandWindows);
#endif
    }

    private ScriptConfigurationT LoadScriptConfig(string scriptConfigPath)
    {
        TomlTable scriptToml = LoadTomlFile(scriptConfigPath);
        string tomlParent = Path.GetDirectoryName(scriptConfigPath) ?? "";

        TomlTable settings = GetValue<TomlTable>(scriptToml, Fields.AgentSettingsTable, []);
        using (_context.Begin(Fields.AgentSettingsTable))
        {
            return new ScriptConfigurationT
            {
                Name = GetValue(settings, Fields.AgentName, ""),
                RootDir = CombinePaths(
                    tomlParent,
                    GetValue(settings, Fields.AgentRootDir, "")
                ),
                RunCommand = GetRunCommand(settings),
                AgentId = GetValue(settings, Fields.AgentAgentId, ""),
            };
        }
    }

    private uint GetTeam(TomlTable table, List<string> missingValues)
    {
        if (!table.TryGetValue(Fields.AgentTeam, out var raw))
        {
            missingValues.Add(_context.ToStringWithEnd(Fields.AgentTeam));
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
                    $"{_context.ToStringWithEnd(Fields.AgentTeam)} has invalid value {raw}. "
                        + $"Use 0, 1, \"blue\", or \"orange\"."
                );
        }
    }

    private PlayerConfigurationT ParseCarTable(TomlTable table, string matchConfigPath)
    {
        var matchConfigDir = Path.GetDirectoryName(matchConfigPath)!;

        uint team = GetTeam(table, _missingValues);
        string? nameOverride = GetValue<string?>(table, Fields.AgentName, null);
        string? loadoutFileOverride = GetValue<string?>(table, Fields.AgentLoadoutFile, null);
        if (!string.IsNullOrEmpty(loadoutFileOverride))
        {
            loadoutFileOverride = Path.Combine(matchConfigDir, loadoutFileOverride);
        }

        PlayerClass playerClass = GetEnum(table, Fields.AgentType, PlayerClass.CustomBot);

        (PlayerClassUnion variety, bool useConfig) = playerClass switch
        {
            PlayerClass.CustomBot => (PlayerClassUnion.FromCustomBot(new CustomBotT()), true),
            PlayerClass.Psyonix => (
                PlayerClassUnion.FromPsyonix(
                    new PsyonixT
                    {
                        BotSkill = GetEnum(table, Fields.AgentSkill, PsyonixSkill.AllStar),
                    }
                ),
                true
            ),
            PlayerClass.Human => (PlayerClassUnion.FromHuman(new HumanT()), false),
            PlayerClass.PartyMember => throw new NotImplementedException(
                "PartyMember not implemented"
            ),
            _ => throw new ConfigParserException(
                $"{_context.ToStringWithEnd(Fields.AgentType)} is out of range."
            ),
        };

        string configPath = useConfig ? GetValue(table, Fields.AgentConfigFile, "") : "";
        // FIXME: Remove in v5.beta.6.0+
        if (configPath == "" && table.ContainsKey(Fields.AgentConfigFileOld))
        {
            Logger.LogError(
                $"In {_context}: '{Fields.AgentConfigFileOld}' has been removed. Use '{Fields.AgentConfigFile}' instead."
            );
        }

        PlayerConfigurationT player;
        if (useConfig && configPath == "" && variety.Type == PlayerClass.CustomBot)
        {
            throw new FileNotFoundException(
                $"{_context} has type \"rlbot\" but {_context.ToStringWithEnd(Fields.AgentConfigFile)} is empty. "
                    + $"RLBot bots must specify a config file."
            );
        }

        if (useConfig && configPath != "")
        {
            string absoluteConfigPath = Path.Combine(matchConfigDir, configPath);
            using (_context.Begin(Fields.AgentConfigFile, ConfigContextTracker.Type.Link))
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
                using (_context.Begin(Fields.AgentLoadoutFile, ConfigContextTracker.Type.Link))
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
                PlayerId = 0,
            };
        }

        bool autoStart = GetValue(table, Fields.AgentAutoStart, true);
        if (!autoStart)
        {
            player.RunCommand = "";
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

        TomlTable settings = GetValue<TomlTable>(table, Fields.AgentSettingsTable, []);
        using (_context.Begin(Fields.AgentSettingsTable))
        {
            string rootDir = Path.Combine(
                configDir,
                GetValue<string>(settings, Fields.AgentRootDir, "")
            );

            // Override is null, "", or an absolute path.
            // Null implies no override and "" implies we should not load the loadout.
            string? loadoutPath = loadoutFileOverride;
            if (loadoutFileOverride is null)
            {
                if (settings.TryGetValue(Fields.AgentLoadoutFile, out var loadoutPathRel))
                {
                    loadoutPath = Path.Combine(configDir, (string)loadoutPathRel);
                }
                else
                {
                    _missingValues.Add(_context.ToStringWithEnd(Fields.AgentLoadoutFile));
                }
            }

            PlayerLoadoutT? loadout;
            using (_context.Begin(Fields.AgentLoadoutFile, ConfigContextTracker.Type.Link))
            {
                loadout =
                    (loadoutPath ?? "") != "" ? LoadPlayerLoadout(loadoutPath!, team) : null;
            }

            return new PlayerConfigurationT
            {
                AgentId = GetValue<string>(settings, Fields.AgentAgentId, ""),
                Name = nameOverride ?? GetValue<string>(settings, Fields.AgentName, ""),
                Team = team,
                Loadout = loadout,
                RunCommand = GetRunCommand(settings),
                Hivemind = GetValue(settings, Fields.AgentHivemind, false),
                RootDir = rootDir,
                PlayerId = 0,
                Variety = variety,
            };
        }
    }

    private PlayerLoadoutT LoadPlayerLoadout(string loadoutPath, uint team)
    {
        TomlTable loadoutToml = LoadTomlFile(loadoutPath);

        string teamLoadoutString =
            team == Team.Blue ? Fields.LoadoutBlueTable : Fields.LoadoutOrangeTable;
        TomlTable teamLoadout = GetValue<TomlTable>(loadoutToml, teamLoadoutString, []);
        using (_context.Begin(teamLoadoutString, ConfigContextTracker.Type.Link))
        {
            TomlTable teamPaint = GetValue<TomlTable>(
                teamLoadout,
                Fields.LoadoutPaintTable,
                []
            );
            LoadoutPaintT loadoutPaint;
            using (_context.Begin(Fields.LoadoutPaintTable))
            {
                loadoutPaint = new LoadoutPaintT
                {
                    // TODO - GetPrimary/Secondary color? Do any bots use this?
                    CarPaintId = (uint)
                        GetValue<long>(teamPaint, Fields.LoadoutPaintCarPaintId, 0),
                    DecalPaintId = (uint)
                        GetValue<long>(teamPaint, Fields.LoadoutPaintDecalPaintId, 0),
                    WheelsPaintId = (uint)
                        GetValue<long>(teamPaint, Fields.LoadoutPaintWheelsPaintId, 0),
                    BoostPaintId = (uint)
                        GetValue<long>(teamPaint, Fields.LoadoutPaintBoostPaintId, 0),
                    AntennaPaintId = (uint)
                        GetValue<long>(teamPaint, Fields.LoadoutPaintAntennaPaintId, 0),
                    HatPaintId = (uint)
                        GetValue<long>(teamPaint, Fields.LoadoutPaintHatPaintId, 0),
                    TrailsPaintId = (uint)
                        GetValue<long>(teamPaint, Fields.LoadoutPaintTrailsPaintId, 0),
                    GoalExplosionPaintId = (uint)
                        GetValue<long>(teamPaint, Fields.LoadoutPaintGoalExplosionPaintId, 0),
                };
            }

            return new PlayerLoadoutT()
            {
                TeamColorId = (uint)GetValue<long>(teamLoadout, Fields.LoadoutTeamColorId, 0),
                CustomColorId = (uint)
                    GetValue<long>(teamLoadout, Fields.LoadoutCustomColorId, 0),
                CarId = (uint)GetValue<long>(teamLoadout, Fields.LoadoutCarId, 0),
                DecalId = (uint)GetValue<long>(teamLoadout, Fields.LoadoutDecalId, 0),
                WheelsId = (uint)GetValue<long>(teamLoadout, Fields.LoadoutWheelsId, 0),
                BoostId = (uint)GetValue<long>(teamLoadout, Fields.LoadoutBoostId, 0),
                AntennaId = (uint)GetValue<long>(teamLoadout, Fields.LoadoutAntennaId, 0),
                HatId = (uint)GetValue<long>(teamLoadout, Fields.LoadoutHatId, 0),
                PaintFinishId = (uint)
                    GetValue<long>(teamLoadout, Fields.LoadoutPaintFinishId, 0),
                CustomFinishId = (uint)
                    GetValue<long>(teamLoadout, Fields.LoadoutCustomFinishId, 0),
                EngineAudioId = (uint)
                    GetValue<long>(teamLoadout, Fields.LoadoutEngineAudioId, 0),
                TrailsId = (uint)GetValue<long>(teamLoadout, Fields.LoadoutTrailsId, 0),
                GoalExplosionId = (uint)
                    GetValue<long>(teamLoadout, Fields.LoadoutGoalExplosionId, 0),
                LoadoutPaint = loadoutPaint,
            };
        }
    }

    private MutatorSettingsT GetMutatorSettings(TomlTable mutatorTable) =>
        new MutatorSettingsT
        {
            MatchLength = GetEnum(
                mutatorTable,
                Fields.MutatorsMatchLength,
                MatchLengthMutator.FiveMinutes
            ),
            MaxScore = GetEnum(
                mutatorTable,
                Fields.MutatorsMaxScore,
                MaxScoreMutator.Unlimited
            ),
            MultiBall = GetEnum(mutatorTable, Fields.MutatorsMultiBall, MultiBallMutator.One),
            Overtime = GetEnum(
                mutatorTable,
                Fields.MutatorsOvertime,
                OvertimeMutator.Unlimited
            ),
            GameSpeed = GetEnum(
                mutatorTable,
                Fields.MutatorsGameSpeed,
                GameSpeedMutator.Default
            ),
            BallMaxSpeed = GetEnum(
                mutatorTable,
                Fields.MutatorsBallMaxSpeed,
                BallMaxSpeedMutator.Default
            ),
            BallType = GetEnum(mutatorTable, Fields.MutatorsBallType, BallTypeMutator.Default),
            BallWeight = GetEnum(
                mutatorTable,
                Fields.MutatorsBallWeight,
                BallWeightMutator.Default
            ),
            BallSize = GetEnum(mutatorTable, Fields.MutatorsBallSize, BallSizeMutator.Default),
            BallBounciness = GetEnum(
                mutatorTable,
                Fields.MutatorsBallBounciness,
                BallBouncinessMutator.Default
            ),
            BoostAmount = GetEnum(
                mutatorTable,
                Fields.MutatorsBoostAmount,
                BoostAmountMutator.NormalBoost
            ),
            Rumble = GetEnum(mutatorTable, Fields.MutatorsRumble, RumbleMutator.Off),
            BoostStrength = GetEnum(
                mutatorTable,
                Fields.MutatorsBoostStrength,
                BoostStrengthMutator.One
            ),
            Gravity = GetEnum(mutatorTable, Fields.MutatorsGravity, GravityMutator.Default),
            Demolish = GetEnum(mutatorTable, Fields.MutatorsDemolish, DemolishMutator.Default),
            RespawnTime = GetEnum(
                mutatorTable,
                Fields.MutatorsRespawnTime,
                RespawnTimeMutator.ThreeSeconds
            ),
            MaxTime = GetEnum(mutatorTable, Fields.MutatorsMaxTime, MaxTimeMutator.Unlimited),
            GameEvent = GetEnum(
                mutatorTable,
                Fields.MutatorsGameEvent,
                GameEventMutator.Default
            ),
            Audio = GetEnum(mutatorTable, Fields.MutatorsAudio, AudioMutator.Default),
            BallGravity = GetEnum(
                mutatorTable,
                Fields.MutatorsBallGravity,
                BallGravityMutator.Default
            ),
            Territory = GetEnum(mutatorTable, Fields.MutatorsTerritory, TerritoryMutator.Off),
            StaleBall = GetEnum(
                mutatorTable,
                Fields.MutatorsStaleBall,
                StaleBallMutator.Unlimited
            ),
            Jump = GetEnum(mutatorTable, Fields.MutatorsJump, JumpMutator.Default),
            DodgeTimer = GetEnum(
                mutatorTable,
                Fields.MutatorsDodgeTimer,
                DodgeTimerMutator.OnePointTwentyFiveSeconds
            ),
            PossessionScore = GetEnum(
                mutatorTable,
                Fields.MutatorsPossessionScore,
                PossessionScoreMutator.Off
            ),
            DemolishScore = GetEnum(
                mutatorTable,
                Fields.MutatorsDemolishScore,
                DemolishScoreMutator.Zero
            ),
            NormalGoalScore = GetEnum(
                mutatorTable,
                Fields.MutatorsNormalGoalScore,
                NormalGoalScoreMutator.One
            ),
            AerialGoalScore = GetEnum(
                mutatorTable,
                Fields.MutatorsAerialGoalScore,
                AerialGoalScoreMutator.One
            ),
            AssistGoalScore = GetEnum(
                mutatorTable,
                Fields.MutatorsAssistGoalScore,
                AssistGoalScoreMutator.Zero
            ),
            InputRestriction = GetEnum(
                mutatorTable,
                Fields.MutatorsInputRestriction,
                InputRestrictionMutator.Default
            ),
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

            TomlTable rlbotTable = GetValue<TomlTable>(outerTable, Fields.RlBotTable, []);
            using (_context.Begin(Fields.RlBotTable))
            {
                matchConfig.Launcher = GetEnum(
                    rlbotTable,
                    Fields.RlBotLauncher,
                    Launcher.Steam
                );
                matchConfig.LauncherArg = GetValue(rlbotTable, Fields.RlBotLauncherArg, "");
                matchConfig.AutoStartAgents = GetValue(
                    rlbotTable,
                    Fields.RlBotAutoStartAgents,
                    true
                );
                matchConfig.WaitForAgents = GetValue(
                    rlbotTable,
                    Fields.RlBotWaitForAgents,
                    true
                );
                // TODO: Remove in future version
                if (rlbotTable.ContainsKey(Fields.RlBotAutoStartAgentsOld))
                {
                    bool autoStartBots = GetValue(
                        rlbotTable,
                        Fields.RlBotAutoStartAgentsOld,
                        true
                    );
                    matchConfig.AutoStartAgents = autoStartBots;
                    matchConfig.WaitForAgents = autoStartBots;
                    Logger.LogWarning(
                        $"'{Fields.RlBotAutoStartAgentsOld}' is deprecated. Please use "
                            + $"'{Fields.RlBotAutoStartAgents}' and '{Fields.RlBotWaitForAgents}' instead."
                    );
                }
            }

            TomlTableArray players = GetValue<TomlTableArray>(outerTable, Fields.CarsList, []);
            matchConfig.PlayerConfigurations = [];
            for (var i = 0; i < players.Count; i++)
            {
                using (_context.Begin($"{Fields.CarsList}[{i}]"))
                {
                    matchConfig.PlayerConfigurations.Add(ParseCarTable(players[i], path));
                }
            }

            TomlTableArray scripts = GetValue<TomlTableArray>(
                outerTable,
                Fields.ScriptsList,
                []
            );
            matchConfig.ScriptConfigurations = [];
            for (var i = 0; i < scripts.Count; i++)
            {
                using (_context.Begin($"{Fields.ScriptsList}[{i}]"))
                {
                    string configPath = GetValue(scripts[i], Fields.AgentConfigFile, "");
                    if (configPath != "")
                    {
                        string absoluteConfigPath = Path.Combine(
                            Path.GetDirectoryName(path)!,
                            configPath
                        );
                        using (
                            _context.Begin(
                                Fields.AgentConfigFile,
                                ConfigContextTracker.Type.Link
                            )
                        )
                        {
                            var script = LoadScriptConfig(absoluteConfigPath);

                            bool autoStart = GetValue(scripts[i], Fields.AgentAutoStart, true);
                            if (!autoStart)
                            {
                                script.RunCommand = "";
                            }

                            matchConfig.ScriptConfigurations.Add(script);
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException(
                            $"{_context.ToStringWithEnd(Fields.AgentConfigFile)} is empty. "
                                + $"Scripts must specify a config file."
                        );
                    }
                }
            }

            TomlTable mutatorTable = GetValue<TomlTable>(outerTable, Fields.MutatorsTable, []);
            using (_context.Begin(Fields.MutatorsTable))
            {
                matchConfig.Mutators = GetMutatorSettings(mutatorTable);
            }

            TomlTable matchTable = GetValue<TomlTable>(outerTable, Fields.MatchTable, []);
            using (_context.Begin(Fields.MatchTable))
            {
                matchConfig.GameMode = GetEnum(
                    matchTable,
                    Fields.MatchGameMode,
                    GameMode.Soccer
                );
                matchConfig.GameMapUpk = GetValue(matchTable, Fields.MatchMapUpk, "Stadium_P");
                matchConfig.SkipReplays = GetValue(matchTable, Fields.MatchSkipReplays, false);
                matchConfig.InstantStart = GetValue(
                    matchTable,
                    Fields.MatchStartWithoutCountdown,
                    false
                );
                matchConfig.EnableRendering = GetValue(
                    matchTable,
                    Fields.MatchRendering,
                    false
                );
                matchConfig.EnableStateSetting = GetValue(
                    matchTable,
                    Fields.MatchStateSetting,
                    true
                );
                matchConfig.ExistingMatchBehavior = GetEnum(
                    matchTable,
                    Fields.MatchExistingMatchBehavior,
                    ExistingMatchBehavior.Restart
                );
                matchConfig.AutoSaveReplay = GetValue(
                    matchTable,
                    Fields.MatchAutoSaveReplays,
                    false
                );
                matchConfig.Freeplay = GetValue(matchTable, Fields.MatchFreePlay, false);
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
