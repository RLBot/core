﻿using System.Diagnostics;
using System.Threading.Channels;
using Bridge.Models.Message;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.Server.BridgeMessage;
using ConsoleCommand = RLBotCS.Server.BridgeMessage.ConsoleCommand;

namespace RLBotCS.ManagerTools;

class MatchStarter(ChannelWriter<IBridgeMessage> bridge, int gamePort, int rlbotSocketsPort)
{
    private static readonly ILogger Logger = Logging.GetLogger("MatchStarter");

    private MatchConfigurationT? _deferredMatchConfig;
    private MatchConfigurationT? _matchConfig;
    private Dictionary<string, string> _hivemindNameMap = new();
    private int _expectedConnections;
    private int _connectionReadies;

    private bool _communicationStarted;
    private bool _needsSpawnCars;

    public bool HasSpawnedMap;
    public bool MatchEnded;

    public MatchConfigurationT? GetMatchConfig() => _deferredMatchConfig ?? _matchConfig;

    public void SetMatchConfigNull()
    {
        if (!_needsSpawnCars)
            _matchConfig = null;
    }

    public void StartCommunication()
    {
        _communicationStarted = true;
        if (_deferredMatchConfig is { } matchConfig)
            LoadMatch(matchConfig);
    }

    public void StartMatch(MatchConfigurationT matchConfig)
    {
        PreprocessMatch(matchConfig);

        if (!LaunchManager.IsRocketLeagueRunningWithArgs())
        {
            _communicationStarted = false;
            LaunchManager.LaunchRocketLeague(
                matchConfig.Launcher,
                matchConfig.LauncherArg,
                gamePort
            );
        }

        if (!_communicationStarted)
        {
            // Defer the message
            _deferredMatchConfig = matchConfig;
            return;
        }

        LoadMatch(matchConfig);
    }

    public void MapSpawned(string MapName)
    {
        Logger.LogInformation("Got map info for " + MapName);
        HasSpawnedMap = true;

        if (!_needsSpawnCars)
            return;

        if (_deferredMatchConfig is { } matchConfig)
        {
            bridge.TryWrite(new SetMutators(matchConfig.Mutators));

            bool spawned = SpawnCars(matchConfig);
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
                _hivemindNameMap[playerConfig.Name] = playerName;

            if (playerConfig.SpawnId == 0)
            {
                playerConfig.SpawnId = $"${playerConfig.AgentId}/${playerConfig.Team}/${i}".GetHashCode();
            }

            playerConfig.RunCommand ??= "";
            Debug.Assert(
                (playerConfig.RootDir ??= "") != "",
                "Root directory must be non-empty at this point."
            );
            Debug.Assert((playerConfig.AgentId ??= "") != "", "Agent ids may not be empty.");
        }

        Dictionary<string, int> scriptNames = [];
        foreach (var scriptConfig in matchConfig.ScriptConfigurations)
        {
            // De-duplicating similar names, Overwrites original value
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

            scriptConfig.RootDir ??= "";
            scriptConfig.RunCommand ??= "";
            scriptConfig.AgentId ??= "";
        }

        matchConfig.LauncherArg ??= "";
        matchConfig.GameMapUpk ??= "";
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

        _connectionReadies = 0;
        _expectedConnections = matchConfig.ScriptConfigurations.Count + processes.Count;

        if (matchConfig.AutoStartBots)
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

    private void LoadMatch(MatchConfigurationT matchConfig)
    {
        StartBotsAndScripts(matchConfig);

        if (matchConfig.AutoSaveReplay)
            bridge.TryWrite(new ConsoleCommand(FlatToCommand.MakeAutoSaveReplayCommand()));

        var shouldSpawnNewMap = matchConfig.ExistingMatchBehavior switch
        {
            ExistingMatchBehavior.ContinueAndSpawn => false,
            ExistingMatchBehavior.RestartIfDifferent => MatchEnded
                || IsDifferentFromLast(matchConfig),
            _ => true,
        };

        _needsSpawnCars = true;
        if (shouldSpawnNewMap)
        {
            HasSpawnedMap = false;
            _matchConfig = null;
            _deferredMatchConfig = matchConfig;

            bridge.TryWrite(new SpawnMap(matchConfig));
        }
        else
        {
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
                        despawnHuman = !matchConfig.PlayerConfigurations.Any(
                            p => p.Variety.Type == PlayerClass.Human
                        );
                        toDespawnNames.Add($"human (index {i}, team {lastPlayerConfig.Team})");
                        continue;
                    }

                    if (matchConfig.PlayerConfigurations.Count <= i)
                    {
                        toDespawnIds.Add(lastPlayerConfig.SpawnId);
                        toDespawnNames.Add($"{lastPlayerConfig.AgentId} (index {i}, team {lastPlayerConfig.Team})");
                        continue;
                    }

                    var playerConfig = matchConfig.PlayerConfigurations[i];
                    if (
                        lastPlayerConfig.AgentId != playerConfig.AgentId
                        || lastPlayerConfig.Team != playerConfig.Team
                    )
                    {
                        toDespawnIds.Add(lastPlayerConfig.SpawnId);
                        toDespawnNames.Add($"{lastPlayerConfig.AgentId} (index {i}, team {lastPlayerConfig.Team})");
                    }
                }

                if (despawnHuman || toDespawnIds.Count > 0)
                {
                    Logger.LogInformation("Despawning old player(s): " + string.Join(", ", toDespawnNames));

                    if (despawnHuman)
                    {
                        bridge.TryWrite(new ConsoleCommand("spectate"));
                    }
                    if (toDespawnIds.Count > 0)
                    {
                        bridge.TryWrite(new RemoveOldPlayers(toDespawnIds));
                    }
                    
                    bridge.TryWrite(new FlushMatchCommands());
                }
            }

            // No need to load a new map, just spawn the players.
            SpawnCars(matchConfig, true);
            bridge.TryWrite(new FlushMatchCommands());

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

    private bool SpawnCars(MatchConfigurationT matchConfig, bool force = false)
    {
        // ensure this function is only called once
        // and only if the map has been spawned
        if (!force && (!_needsSpawnCars || !HasSpawnedMap))
            return false;

        bool doSpawning =
            force || !matchConfig.AutoStartBots || _expectedConnections <= _connectionReadies;
        Logger.LogInformation(
            "Spawning cars: "
                + _expectedConnections
                + " expected connections, "
                + _connectionReadies
                + " connection readies, "
                + (doSpawning ? "spawning" : "not spawning")
        );

        if (!doSpawning)
            return false;

        _needsSpawnCars = false;

        PlayerConfigurationT? humanConfig = null;
        int numPlayers = matchConfig.PlayerConfigurations.Count;
        int indexOffset = 0;

        for (int i = 0; i < numPlayers; i++)
        {
            var playerConfig = matchConfig.PlayerConfigurations[i];

            switch (playerConfig.Variety.Type)
            {
                case PlayerClass.CustomBot:
                    Logger.LogInformation(
                        "Spawning player "
                            + playerConfig.Name
                            + " with agent id "
                            + playerConfig.AgentId
                    );

                    bridge.TryWrite(
                        new SpawnBot(
                            playerConfig,
                            BotSkill.Custom,
                            (uint)(i - indexOffset),
                            true
                        )
                    );

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

                    bridge.TryWrite(
                        new SpawnBot(playerConfig, skillEnum, (uint)(i - indexOffset), false)
                    );

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
            bridge.TryWrite(new ConsoleCommand("spectate"));
        else
            bridge.TryWrite(new SpawnHuman(humanConfig, (uint)(numPlayers - indexOffset)));

        bridge.TryWrite(new MarkQueuingComplete());

        return true;
    }

    public void AddLoadout(PlayerLoadoutT loadout, int spawnId)
    {
        var matchConfig = _deferredMatchConfig ?? _matchConfig;
        if (matchConfig is null)
        {
            Logger.LogError("Match settings not loaded yet.");
            return;
        }

        if (!_needsSpawnCars)
        {
            // todo: when the match is already running,
            // respawn the car with the new loadout in the same position
            Logger.LogError(
                "Match already started, can't add loadout - feature has not implemented!"
            );
            return;
        }

        var player = matchConfig.PlayerConfigurations.Find(p => p.SpawnId == spawnId);
        if (player is null)
        {
            Logger.LogError($"Player with spawn id {spawnId} not found to add loadout to.");
            return;
        }

        if (player.Loadout is not null)
        {
            Logger.LogError(
                $"Player \"{player.Name}\" with spawn id {spawnId} already has a loadout."
            );
            return;
        }

        player.Loadout = loadout;
    }

    public void IncrementConnectionReadies()
    {
        _connectionReadies++;

        Logger.LogInformation(
            "Connection readies: "
                + _connectionReadies
                + " / "
                + _expectedConnections
                + "; needs spawn cars: "
                + _needsSpawnCars
        );

        if (
            _deferredMatchConfig is { } matchConfig
            && _connectionReadies >= _expectedConnections
            && _needsSpawnCars
        )
        {
            bool spawned = SpawnCars(matchConfig);
            if (!spawned)
                return;

            _matchConfig = matchConfig;
            _deferredMatchConfig = null;
        }
    }
}
