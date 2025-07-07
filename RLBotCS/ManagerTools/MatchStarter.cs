using System.Diagnostics;
using Bridge.Models.Message;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using MatchPhase = Bridge.Models.Message.MatchPhase;

namespace RLBotCS.ManagerTools;

class MatchStarter(int gamePort, int rlbotSocketsPort)
{
    private static readonly ILogger Logger = Logging.GetLogger("MatchStarter");

    /// <summary>
    /// If not null, then we are waiting for RL to load the map of this config.
    /// Once loaded, we will do car spawning, etc.
    /// </summary>
    private MatchConfigurationT? _deferredMatchConfig;

    /// <summary>
    /// The most recently loaded match.
    /// </summary>
    private MatchConfigurationT? _matchConfig;

    private Dictionary<string, string> _hivemindNameMap = new();

    /// <summary>
    /// If this value is not null,
    /// then a custom map is being loaded.
    /// </summary>
    private CustomMap? _customMap;

    public readonly AgentMapping AgentMapping = new();

    public bool HasSpawnedCars { get; private set; }
    public bool HasSpawnedMap { get; private set; }

    /// <summary>Match phase of the most recently started match.
    /// If null, we have not heard from RL yet (we might not be connected).</summary>
    private MatchPhase? _currentMatchPhase;

    public MatchConfigurationT? GetMatchConfig() => _deferredMatchConfig ?? _matchConfig;

    public void ResetMatchStarting()
    {
        Logger.LogDebug("Reset MatchStarter");
        _deferredMatchConfig = null;
        _matchConfig = null;
        HasSpawnedMap = false;
        HasSpawnedCars = false;
    }

    public void SetCurrentMatchPhase(MatchPhase phase, PlayerSpawner spawner)
    {
        bool phaseWasNull = _currentMatchPhase == null;
        if (_currentMatchPhase != phase)
        {
            _currentMatchPhase = phase;
            Logger.LogDebug($"Match Phase: {_currentMatchPhase}");
        }

        // If phase was null, then connection was just established.
        if (phaseWasNull && _deferredMatchConfig is { } matchConfig)
        {
            if (
                matchConfig.ExistingMatchBehavior == ExistingMatchBehavior.ContinueAndSpawn
                && phase is MatchPhase.Inactive or MatchPhase.Ended
            )
            {
                Logger.LogWarning(
                    "ContinueAndSpawn failed because no match is active. Starting a match instead."
                );
                matchConfig.ExistingMatchBehavior = ExistingMatchBehavior.Restart;
            }

            LoadMatch(matchConfig, spawner);
        }
    }

    public void StartMatch(MatchConfigurationT matchConfig, PlayerSpawner spawner)
    {
        PreprocessMatch(matchConfig);

        if (!LaunchManager.IsRocketLeagueRunningWithArgs())
        {
            _currentMatchPhase = null;
            LaunchManager.LaunchRocketLeague(
                matchConfig.Launcher,
                matchConfig.LauncherArg,
                gamePort
            );

            if (matchConfig.ExistingMatchBehavior == ExistingMatchBehavior.ContinueAndSpawn)
            {
                Logger.LogWarning(
                    "ContinueAndSpawn failed since RL is not running. Starting a match instead."
                );
                matchConfig.ExistingMatchBehavior = ExistingMatchBehavior.Restart;
            }
        }

        if (_currentMatchPhase == null)
        {
            // Defer start, since we are not connected to RL yet
            ResetMatchStarting();
            _deferredMatchConfig = matchConfig;
            return;
        }

        LoadMatch(matchConfig, spawner);
    }

    public void OnMapSpawn(string mapName, PlayerSpawner spawner)
    {
        Logger.LogInformation("Got map info for " + mapName);
        HasSpawnedMap = true;
        if (_customMap is not null)
        {
            _customMap.TryRestoreOriginalMap();
            _customMap = null;
        }

        if (_deferredMatchConfig is { } matchConfig)
        {
            bool spawned = SpawnCars(matchConfig, spawner);
            if (!spawned)
                return;

            _matchConfig = matchConfig;
            _deferredMatchConfig = null;
        }
    }

    private void PreprocessMatch(MatchConfigurationT matchConfig)
    {
        Dictionary<string, int> playerNames = [];
        _hivemindNameMap.Clear();

        foreach (var player in matchConfig.PlayerConfigurations)
        {
            // De-duplicating similar names. Overwrites original value.

            if (player.Variety.Value is CustomBotT config)
            {
                string playerName = config.Name ?? "";
                if (playerNames.TryGetValue(playerName, out int value))
                {
                    playerNames[playerName] = ++value;
                    config.Name = playerName + $" ({value + 1})";
                }
                else
                {
                    playerNames[playerName] = 0;
                    config.Name = playerName;
                }

                if (config.Hivemind)
                {
                    _hivemindNameMap[config.Name] = playerName;
                }
            }
        }

        Dictionary<string, int> scriptNames = [];
        foreach (var scriptConfig in matchConfig.ScriptConfigurations)
        {
            // De-duplicating similar names. Overwrites original value.
            string scriptName = scriptConfig.Name ?? "";
            if (scriptNames.TryGetValue(scriptName, out int value))
            {
                scriptNames[scriptName] = ++value;
                scriptConfig.Name = scriptName + $" ({value})";
            }
            else
            {
                scriptNames[scriptName] = 0;
                scriptConfig.Name = scriptName;
            }
        }
    }

    private void StartBotsAndScripts(MatchConfigurationT matchConfig)
    {
        Dictionary<string, PlayerConfigurationT> processes = new();

        foreach (var player in matchConfig.PlayerConfigurations)
        {
            if (player.Variety.Value is CustomBotT config)
            {
                if (config.Hivemind)
                {
                    // only add one process per team
                    // make sure to not accidentally include two bots
                    // with the same names in the same hivemind process
                    string uniqueName =
                        config.RootDir
                        + "_"
                        + config.RunCommand
                        + "_"
                        + _hivemindNameMap[config.Name]
                        + "_"
                        + player.Team;

                    if (!processes.ContainsKey(uniqueName))
                        processes[uniqueName] = player;
                }
                else
                {
                    processes[config.Name] = player;
                }
            }
        }

        _hivemindNameMap.Clear();

        if (matchConfig.AutoStartAgents)
        {
            LaunchManager.LaunchBots(processes.Values.ToList(), rlbotSocketsPort);
            LaunchManager.LaunchScripts(matchConfig.ScriptConfigurations, rlbotSocketsPort);
        }
        else
        {
            Logger.LogWarning(
                "AutoStartBots is disabled in match settings. Bots & scripts will not be started automatically!"
            );
        }
    }

    private void LoadMatch(MatchConfigurationT matchConfig, PlayerSpawner spawner)
    {
        StartBotsAndScripts(matchConfig);

        var matchInactive =
            _currentMatchPhase is null or MatchPhase.Inactive or MatchPhase.Ended;
        var shouldSpawnNewMap = matchConfig.ExistingMatchBehavior switch
        {
            ExistingMatchBehavior.ContinueAndSpawn => matchInactive,
            ExistingMatchBehavior.RestartIfDifferent => matchInactive
                || IsDifferentFromLast(matchConfig),
            _ => true,
        };

        if (
            matchConfig.ExistingMatchBehavior == ExistingMatchBehavior.ContinueAndSpawn
            && shouldSpawnNewMap
        )
        {
            Logger.LogWarning(
                "ContinueAndSpawn failed since no match is running. Starting a match instead."
            );
        }

        HasSpawnedMap = !shouldSpawnNewMap;
        HasSpawnedCars = false;

        if (shouldSpawnNewMap)
        {
            _matchConfig = null;
            _deferredMatchConfig = matchConfig;

            var (cmd, customMap) = spawner.SpawnMap(matchConfig);
            _customMap = customMap;

            Logger.LogInformation($"Loading map with command: {cmd}");
        }
        else
        {
            // Despawn cars that aren't in the new match
            if (_matchConfig is { } lastMatchConfig)
            {
                bool despawnHuman = false;
                List<int> toDespawnIds = new(lastMatchConfig.PlayerConfigurations.Count);
                List<string> toDespawnNames = new(lastMatchConfig.PlayerConfigurations.Count);
                for (var i = 0; i < lastMatchConfig.PlayerConfigurations.Count; i++)
                {
                    var lastPlayerConfig = lastMatchConfig.PlayerConfigurations[i];
                    if (lastPlayerConfig.Variety.Type == PlayerClass.Human)
                    {
                        despawnHuman = !matchConfig.PlayerConfigurations.Any(p =>
                            p.Variety.Type == PlayerClass.Human
                        );
                        toDespawnNames.Add($"human (index {i}, team {lastPlayerConfig.Team})");
                        continue;
                    }

                    string lastAgentId = lastPlayerConfig.Variety.Value switch
                    {
                        PsyonixBotT bot => $"psyonix/{bot.BotSkill}",
                        CustomBotT bot => bot.AgentId,
                        _ => "human",
                    };

                    if (matchConfig.PlayerConfigurations.Count <= i)
                    {
                        toDespawnIds.Add(lastPlayerConfig.PlayerId);
                        toDespawnNames.Add(
                            $"{lastAgentId} (index {i}, team {lastPlayerConfig.Team})"
                        );
                        continue;
                    }

                    var playerConfig = matchConfig.PlayerConfigurations[i];
                    string agentId = playerConfig.Variety.Value switch
                    {
                        PsyonixBotT bot => $"psyonix/{bot.BotSkill}",
                        CustomBotT bot => bot.AgentId,
                        _ => "human",
                    };

                    if (
                        lastPlayerConfig.Variety.Type != playerConfig.Variety.Type
                        || lastAgentId != agentId
                        || lastPlayerConfig.Team != playerConfig.Team
                    )
                    {
                        toDespawnIds.Add(lastPlayerConfig.PlayerId);
                        toDespawnNames.Add(
                            $"{lastAgentId} (index {i}, team {lastPlayerConfig.Team})"
                        );
                    }
                }

                if (despawnHuman || toDespawnIds.Count > 0)
                {
                    Logger.LogInformation(
                        "Despawning old player(s): " + string.Join(", ", toDespawnNames)
                    );

                    if (despawnHuman)
                    {
                        spawner.MakeHumanSpectate();
                    }
                    if (toDespawnIds.Count > 0)
                    {
                        spawner.DespawnPlayers(toDespawnIds);
                    }

                    // We can flush C&S despawn commands immediately
                    spawner.Flush();
                }
            }

            // Spawning (existing players will not be spawned again)
            SpawnCars(matchConfig, spawner, true);

            _matchConfig = matchConfig;
            _deferredMatchConfig = null;
        }
    }

    private bool IsDifferentFromLast(MatchConfigurationT matchConfig)
    {
        // Don't consider rendering/state setting because that can be enabled/disabled without restarting the match

        var lastMatchConfig = _matchConfig;
        if (lastMatchConfig == null)
            return true;

        if (
            lastMatchConfig.PlayerConfigurations.Count
            != matchConfig.PlayerConfigurations.Count
        )
            return true;

        for (var i = 0; i < lastMatchConfig.PlayerConfigurations.Count; i++)
        {
            var lastPlayerConfig = lastMatchConfig.PlayerConfigurations[i];
            var playerConfig = matchConfig.PlayerConfigurations[i];

            if (
                lastPlayerConfig.Variety.Type != playerConfig.Variety.Type
                || lastPlayerConfig.Team != playerConfig.Team
            )
                return true;

            switch (lastPlayerConfig.Variety.Value)
            {
                case PsyonixBotT lastBot:
                    if (lastBot.BotSkill != playerConfig.Variety.AsPsyonixBot().BotSkill)
                        return true;
                    break;
                case CustomBotT lastBot:
                    if (lastBot.AgentId != playerConfig.Variety.AsCustomBot().AgentId)
                        return true;
                    break;
            }
        }

        var lastMutators = lastMatchConfig.Mutators;
        var mutators = matchConfig.Mutators;

        return lastMatchConfig.Freeplay != matchConfig.Freeplay
            || lastMatchConfig.GameMode != matchConfig.GameMode
            || lastMatchConfig.GameMapUpk != matchConfig.GameMapUpk
            || lastMatchConfig.InstantStart != matchConfig.InstantStart
            || lastMutators.MatchLength != mutators.MatchLength
            || lastMutators.MaxScore != mutators.MaxScore
            || lastMutators.MultiBall != mutators.MultiBall
            || lastMutators.Overtime != mutators.Overtime
            || lastMutators.SeriesLength != mutators.SeriesLength
            || lastMutators.BallMaxSpeed != mutators.BallMaxSpeed
            || lastMutators.BallType != mutators.BallType
            || lastMutators.BallWeight != mutators.BallWeight
            || lastMutators.BallSize != mutators.BallSize
            || lastMutators.BallBounciness != mutators.BallBounciness
            || lastMutators.BoostAmount != mutators.BoostAmount
            || lastMutators.Rumble != mutators.Rumble
            || lastMutators.BoostStrength != mutators.BoostStrength
            || lastMutators.Demolish != mutators.Demolish
            || lastMutators.RespawnTime != mutators.RespawnTime;
    }

    private bool SpawnCars(
        MatchConfigurationT matchConfig,
        PlayerSpawner spawner,
        bool force = false
    )
    {
        // ensure this function is only called once
        // and only if the map has been spawned
        if (!force && (HasSpawnedCars || !HasSpawnedMap))
            return false;

        var (ready, expected) = AgentMapping.GetReadyStatus();
        bool doSpawning = force || !matchConfig.WaitForAgents || ready >= expected;

        if (!doSpawning)
        {
            Logger.LogInformation(
                "Spawning deferred due to unready agents. Ready: " + ready + " / " + expected
            );
            return false;
        }

        HasSpawnedCars = true;

        PlayerConfigurationT? humanConfig = null;
        int numPlayers = matchConfig.PlayerConfigurations.Count;

        for (int i = 0; i < numPlayers; i++)
        {
            var playerConfig = matchConfig.PlayerConfigurations[i];

            switch (playerConfig.Variety.Value)
            {
                case CustomBotT bot:
                    Logger.LogInformation(
                        $"Spawning {bot.Name} (index {i}, team {playerConfig.Team}, aid {bot.AgentId})"
                    );

                    spawner.SpawnBot(playerConfig, BotSkill.Custom, (uint)i);
                    break;
                case PsyonixBotT bot:
                    Logger.LogInformation(
                        $"Spawning {bot.Name} (index {i}, team {playerConfig.Team}, skill {bot.BotSkill})"
                    );

                    var skillEnum = bot.BotSkill switch
                    {
                        PsyonixSkill.Beginner => BotSkill.Intro,
                        PsyonixSkill.Rookie => BotSkill.Easy,
                        PsyonixSkill.Pro => BotSkill.Medium,
                        PsyonixSkill.AllStar => BotSkill.Hard,
                        _ => throw new ArgumentOutOfRangeException(
                            $"{ConfigParser.Fields.CarsList}[{i}].{ConfigParser.Fields.AgentSkill}",
                            "Psyonix skill level is out of range."
                        ),
                    };

                    spawner.SpawnBot(playerConfig, skillEnum, (uint)i);

                    break;
                case HumanT:
                    // This assertion is upheld by the ConfigValidator. We require it, since otherwise
                    // the match config in the server could have a different ordering of players
                    Debug.Assert(
                        i == numPlayers - 1,
                        "Human must be last player in match config."
                    );

                    Logger.LogInformation(
                        $"Spawning human (index {i}, team {playerConfig.Team})"
                    );

                    // Human spawning happens after the loop
                    humanConfig = playerConfig;
                    break;
            }
        }

        // If no human was requested for the match,
        // then make the human spectate so we can start the match
        if (humanConfig is null)
            spawner.MakeHumanSpectate();
        else
            spawner.SpawnHuman(humanConfig, (uint)(numPlayers - 1));

        if (force)
        {
            spawner.Flush();
        }

        return true;
    }

    public void CheckAgentReadyStatus(PlayerSpawner spawner)
    {
        if (_deferredMatchConfig is { } matchConfig && !HasSpawnedCars)
        {
            var (ready, expected) = AgentMapping.GetReadyStatus();
            Logger.LogInformation("Agents ready: " + ready + " / " + expected);

            if (ready >= expected)
            {
                bool spawned = SpawnCars(matchConfig, spawner);
                if (!spawned)
                    return;

                _matchConfig = matchConfig;
                _deferredMatchConfig = null;
            }
        }
        else
        {
            // Something triggered this check even though we are not waiting for match start, so let's log instead
            var (ready, expected) = AgentMapping.GetReadyStatus();
            Logger.LogDebug("Agents ready: " + ready + " / " + expected);
        }
    }
}
