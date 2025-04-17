using System.Threading.Channels;
using Bridge.Controller;
using Bridge.Models.Message;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.Server.BridgeMessage;
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

    public readonly AgentMapping AgentMapping = new();

    public bool HasSpawnedCars { get; private set; }
    public bool HasSpawnedMap { get; private set; }

    /// <summary>Match phase of the most recently started match.
    /// If null, we have not heard from RL yet (we might not be connected).</summary>
    private MatchPhase? _currentMatchPhase;

    public MatchConfigurationT? GetMatchConfig() => _deferredMatchConfig ?? _matchConfig;

    public void ResetMatchStarting()
    {
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
            _deferredMatchConfig = matchConfig;
            return;
        }

        LoadMatch(matchConfig, spawner);
    }

    public void OnMapSpawn(string mapName, PlayerSpawner spawner)
    {
        Logger.LogInformation("Got map info for " + mapName);
        HasSpawnedMap = true;

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

        for (int i = 0; i < matchConfig.PlayerConfigurations.Count; i++)
        {
            var playerConfig = matchConfig.PlayerConfigurations[i];

            // De-duplicating similar names. Overwrites original value.
            if (playerConfig.Variety.Type == PlayerClass.Human)
                continue;

            string playerName = playerConfig.Name ?? "";
            if (playerNames.TryGetValue(playerName, out int value))
            {
                playerNames[playerName] = ++value;
                playerConfig.Name = playerName + $" ({value + 1})";
            }
            else
            {
                playerNames[playerName] = 0;
                playerConfig.Name = playerName;
            }

            if (playerConfig.Hivemind)
            {
                _hivemindNameMap[playerConfig.Name] = playerName;
            }

            if (playerConfig.SpawnId == 0)
            {
                playerConfig.SpawnId =
                    $"${playerConfig.AgentId}/${playerConfig.Team}/${i}".GetHashCode();
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

            if (scriptConfig.SpawnId == 0)
                scriptConfig.SpawnId = scriptConfig.AgentId.GetHashCode();
        }
    }

    private void StartBotsAndScripts(MatchConfigurationT matchConfig)
    {
        Dictionary<string, PlayerConfigurationT> processes = new();

        foreach (var playerConfig in matchConfig.PlayerConfigurations)
        {
            if (playerConfig.Variety.Type != PlayerClass.CustomBot)
                continue;

            if (playerConfig.Hivemind)
            {
                // only add one process per team
                // make sure to not accidentally include two bots
                // with the same names in the same hivemind process
                string uniqueName =
                    playerConfig.RootDir
                    + "_"
                    + playerConfig.RunCommand
                    + "_"
                    + _hivemindNameMap[playerConfig.Name]
                    + "_"
                    + playerConfig.Team;

                if (!processes.ContainsKey(uniqueName))
                    processes[uniqueName] = playerConfig;
            }
            else
            {
                processes[playerConfig.Name] = playerConfig;
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

        HasSpawnedCars = false;
        
        if (shouldSpawnNewMap)
        {
            HasSpawnedMap = false;
            _matchConfig = null;
            _deferredMatchConfig = matchConfig;

            var cmd = spawner.SpawnMap(matchConfig);
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

                    if (matchConfig.PlayerConfigurations.Count <= i)
                    {
                        toDespawnIds.Add(lastPlayerConfig.SpawnId);
                        toDespawnNames.Add(
                            $"{lastPlayerConfig.AgentId} (index {i}, team {lastPlayerConfig.Team})"
                        );
                        continue;
                    }

                    var playerConfig = matchConfig.PlayerConfigurations[i];
                    if (
                        lastPlayerConfig.AgentId != playerConfig.AgentId
                        || lastPlayerConfig.Team != playerConfig.Team
                    )
                    {
                        toDespawnIds.Add(lastPlayerConfig.SpawnId);
                        toDespawnNames.Add(
                            $"{lastPlayerConfig.AgentId} (index {i}, team {lastPlayerConfig.Team})"
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
                lastPlayerConfig.AgentId != playerConfig.AgentId
                || lastPlayerConfig.Team != playerConfig.Team
            )
                return true;
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

    private bool SpawnCars(MatchConfigurationT matchConfig, PlayerSpawner spawner, bool force = false)
    {
        // ensure this function is only called once
        // and only if the map has been spawned
        if (!force && (HasSpawnedCars || !HasSpawnedMap))
            return false;

        var (ready, expected) = AgentMapping.GetReadyStatus();
        bool doSpawning =
            force || !matchConfig.WaitForAgents || ready >= expected;

        if (!doSpawning)
        {
            Logger.LogInformation(
                "Spawning deferred due to unready agents. Ready: "
                    + ready
                    + " / "
                    + expected
            );
            return false;
        }

        HasSpawnedCars = true;

        PlayerConfigurationT? humanConfig = null;
        int numPlayers = matchConfig.PlayerConfigurations.Count;
        int indexOffset = 0;

        for (int i = 0; i < numPlayers; i++)
        {
            var playerConfig = matchConfig.PlayerConfigurations[i];

            Logger.LogInformation(
                $"Spawning {playerConfig.Name} (index {i}, team {playerConfig.Team}, aid {playerConfig.AgentId})"
            );

            switch (playerConfig.Variety.Type)
            {
                case PlayerClass.CustomBot:
                    spawner.SpawnBot(playerConfig, BotSkill.Custom, (uint)(i - indexOffset));

                    break;
                case PlayerClass.Psyonix:
                    var skillEnum = playerConfig.Variety.AsPsyonix().BotSkill switch
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
    
                    spawner.SpawnBot(playerConfig, skillEnum, (uint)(i - indexOffset));

                    break;
                case PlayerClass.Human:
                    // ensure there's no gap in the player indices
                    indexOffset++;

                    if (humanConfig is null)
                    {
                        // We want the human to have the highest index, defer spawning
                        humanConfig = playerConfig;
                        continue;
                    }

                    // We can't spawn this human player,
                    // so we need to -1 for every index after this
                    // to properly set the desired player indices
                    Logger.LogError(
                        "Multiple human players requested. RLBot only supports spawning max one human per match."
                    );

                    break;
            }
        }

        // If no human was requested for the match,
        // then make the human spectate so we can start the match
        if (humanConfig is null)
            spawner.MakeHumanSpectate();
        else
            spawner.SpawnHuman(humanConfig, (uint)(numPlayers - indexOffset));

        spawner.Flush();

        return true;
    }

    public void CheckAgentReadyStatus(PlayerSpawner spawner)
    {
        if (_deferredMatchConfig is { } matchConfig && !HasSpawnedCars)
        {
            var (ready, expected) = AgentMapping.GetReadyStatus();
            Logger.LogInformation(
                "Agents ready: " + ready + " / " + expected
            );

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
            Logger.LogDebug(
                "Agents ready: " + ready + " / " + expected
            );
        }
    }
}
