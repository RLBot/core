using System.Threading.Channels;
using Bridge.Models.Message;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.Server;
using ConsoleCommand = RLBotCS.Server.ConsoleCommand;

namespace RLBotCS.ManagerTools;

internal class MatchStarter(
    ChannelWriter<IBridgeMessage> bridge,
    int gamePort,
    int rlbotSocketsPort
)
{
    private static readonly ILogger Logger = Logging.GetLogger("MatchStarter");

    private MatchSettingsT? _deferredMatchSettings;
    private MatchSettingsT? _matchSettings;
    private int _expectedConnections;
    private int _connectionReadies;

    private bool _communicationStarted;
    private bool _hasEverLoadedMap;
    private bool _needsSpawnCars;

    public bool HasSpawnedMap;
    public bool MatchEnded;

    public MatchSettingsT? GetMatchSettings() => _deferredMatchSettings ?? _matchSettings;

    public void SetNullMatchSettings()
    {
        if (!_needsSpawnCars)
            _matchSettings = null;
    }

    public void StartCommunication()
    {
        _communicationStarted = true;
        if (_deferredMatchSettings is MatchSettingsT matchSettings)
            MakeMatch(matchSettings);
    }

    public void StartMatch(MatchSettingsT matchSettings)
    {
        if (!LaunchManager.IsRocketLeagueRunning())
        {
            _communicationStarted = false;
            LaunchManager.LaunchRocketLeague(matchSettings.Launcher, gamePort);
        }

        if (!_communicationStarted)
        {
            // Defer the message
            _deferredMatchSettings = matchSettings;
            return;
        }

        MakeMatch(matchSettings);
    }

    public void MapSpawned()
    {
        Logger.LogInformation("Got map info");
        _hasEverLoadedMap = true;
        HasSpawnedMap = true;

        if (!_needsSpawnCars)
            return;

        if (_deferredMatchSettings is MatchSettingsT matchSettings)
        {
            bridge.TryWrite(new SetMutators(matchSettings.MutatorSettings));

            bool spawned = SpawnCars(matchSettings);
            if (!spawned)
                return;

            _matchSettings = matchSettings;
            _deferredMatchSettings = null;
        }
    }

    private void MakeMatch(MatchSettingsT matchSettings)
    {
        Dictionary<string, List<PlayerConfigurationT>> processes = new();
        Dictionary<string, int> playerNames = [];

        foreach (var playerConfig in matchSettings.PlayerConfigurations)
        {
            // De-duplicating similar names, Overwrites original value
            string playerName = playerConfig.Name ?? "";
            if (playerNames.TryGetValue(playerName, out int value))
            {
                playerNames[playerName] = ++value;
                playerConfig.Name = playerName + $" ({value})";
            }
            else
            {
                playerNames[playerName] = 0;
                playerConfig.Name = playerName;
            }

            if (playerConfig.SpawnId == 0)
                playerConfig.SpawnId = playerConfig.Name.GetHashCode();

            playerConfig.Location ??= "";
            playerConfig.RunCommand ??= "";

            if (playerConfig.Variety.Type != PlayerClass.RLBot)
                continue;

            if (playerConfig.Hivemind)
            {
                // only add one process per team
                // make sure to not accidentally include two bots
                // with the same names in the same hivemind process
                string uniqueName =
                    playerConfig.Location
                    + "_"
                    + playerConfig.RunCommand
                    + "_"
                    + playerName
                    + "_"
                    + playerConfig.Team;

                if (!processes.ContainsKey(uniqueName))
                {
                    processes[uniqueName] = new List<PlayerConfigurationT>();
                }

                // the process needs _all_ the spawn ids
                processes[uniqueName].Add(playerConfig);
            }
            else
            {
                processes[playerConfig.Name] = new List<PlayerConfigurationT> { playerConfig };
            }
        }

        foreach (var scriptConfig in matchSettings.ScriptConfigurations)
        {
            // De-duplicating similar names, Overwrites original value
            string scriptName = scriptConfig.Name ?? "";
            if (playerNames.TryGetValue(scriptName, out int value))
            {
                playerNames[scriptName] = ++value;
                scriptConfig.Name = scriptName + $" ({value})";
            }
            else
            {
                playerNames[scriptName] = 0;
                scriptConfig.Name = scriptName;
            }

            if (scriptConfig.SpawnId == 0)
                scriptConfig.SpawnId = scriptConfig.Name.GetHashCode();

            scriptConfig.Location ??= "";
            scriptConfig.RunCommand ??= "";
        }

        matchSettings.GamePath ??= "";
        matchSettings.GameMapUpk ??= "";

        _connectionReadies = 0;
        _expectedConnections = matchSettings.ScriptConfigurations.Count + processes.Count;

        if (matchSettings.AutoStartBots)
        {
            LaunchManager.LaunchBots(processes, rlbotSocketsPort);
            LaunchManager.LaunchScripts(matchSettings.ScriptConfigurations, rlbotSocketsPort);
        }

        LoadMatch(matchSettings);
    }

    private void LoadMatch(MatchSettingsT matchSettings)
    {
        if (matchSettings.AutoSaveReplay)
            bridge.TryWrite(new ConsoleCommand(FlatToCommand.MakeAutoSaveReplayCommand()));

        var shouldSpawnNewMap = matchSettings.ExistingMatchBehavior switch
        {
            ExistingMatchBehavior.Continue_And_Spawn => !_hasEverLoadedMap || MatchEnded,
            ExistingMatchBehavior.Restart_If_Different
                => MatchEnded || IsDifferentFromLast(matchSettings),
            _ => true
        };

        _needsSpawnCars = true;
        if (shouldSpawnNewMap)
        {
            _hasEverLoadedMap = true;
            HasSpawnedMap = false;
            _matchSettings = null;
            _deferredMatchSettings = matchSettings;

            bridge.TryWrite(new SpawnMap(matchSettings));
        }
        else
        {
            // despawn old bots that aren't in the new match
            if (_matchSettings is MatchSettingsT lastMatchSettings)
            {
                var lastSpawnIds = lastMatchSettings
                    .PlayerConfigurations.Select(p => p.SpawnId)
                    .ToList();
                var currentSpawnIds = matchSettings
                    .PlayerConfigurations.Select(p => p.SpawnId)
                    .ToList();
                var toDespawn = lastSpawnIds.Except(currentSpawnIds).ToList();

                if (toDespawn.Count > 0)
                {
                    Logger.LogInformation(
                        "Despawning old players: " + string.Join(", ", toDespawn)
                    );
                    bridge.TryWrite(new RemoveOldPlayers(toDespawn));
                }
            }

            // No need to load a new map, just spawn the players.
            SpawnCars(matchSettings, true);
            bridge.TryWrite(new FlushMatchCommands());

            _matchSettings = matchSettings;
            _deferredMatchSettings = null;
        }
    }

    private bool IsDifferentFromLast(MatchSettingsT matchSettings)
    {
        // Don't consider rendering/state setting because that can be enabled/disabled without restarting the match

        var lastMatchSettings = _matchSettings;
        if (lastMatchSettings == null)
            return true;

        if (
            lastMatchSettings.PlayerConfigurations.Count
            != matchSettings.PlayerConfigurations.Count
        )
            return true;

        for (var i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
        {
            var lastPlayerConfig = lastMatchSettings.PlayerConfigurations[i];
            var playerConfig = matchSettings.PlayerConfigurations[i];

            if (
                lastPlayerConfig.SpawnId != playerConfig.SpawnId
                || lastPlayerConfig.Team != playerConfig.Team
            )
                return true;
        }

        var lastMutators = lastMatchSettings.MutatorSettings;
        var mutators = matchSettings.MutatorSettings;

        return lastMatchSettings.Freeplay != matchSettings.Freeplay
            || lastMatchSettings.GameMode != matchSettings.GameMode
            || lastMatchSettings.GameMapUpk != matchSettings.GameMapUpk
            || lastMatchSettings.InstantStart != matchSettings.InstantStart
            || lastMutators.MatchLength != mutators.MatchLength
            || lastMutators.MaxScore != mutators.MaxScore
            || lastMutators.MultiBall != mutators.MultiBall
            || lastMutators.OvertimeOption != mutators.OvertimeOption
            || lastMutators.SeriesLengthOption != mutators.SeriesLengthOption
            || lastMutators.BallMaxSpeedOption != mutators.BallMaxSpeedOption
            || lastMutators.BallTypeOption != mutators.BallTypeOption
            || lastMutators.BallWeightOption != mutators.BallWeightOption
            || lastMutators.BallSizeOption != mutators.BallSizeOption
            || lastMutators.BallBouncinessOption != mutators.BallBouncinessOption
            || lastMutators.BoostOption != mutators.BoostOption
            || lastMutators.RumbleOption != mutators.RumbleOption
            || lastMutators.BoostStrengthOption != mutators.BoostStrengthOption
            || lastMutators.DemolishOption != mutators.DemolishOption
            || lastMutators.RespawnTimeOption != mutators.RespawnTimeOption;
    }

    private bool SpawnCars(MatchSettingsT matchSettings, bool force = false)
    {
        // ensure this function is only called once
        // and only if the map has been spawned
        if (!_needsSpawnCars || !HasSpawnedMap)
            return false;

        bool doSpawning =
            force
            || !matchSettings.AutoStartBots
            || (_expectedConnections != 0 && _expectedConnections <= _connectionReadies);
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
        int numPlayers = matchSettings.PlayerConfigurations.Count;
        int indexOffset = 0;

        for (int i = 0; i < numPlayers; i++)
        {
            var playerConfig = matchSettings.PlayerConfigurations[i];

            switch (playerConfig.Variety.Type)
            {
                case PlayerClass.RLBot:
                    Logger.LogInformation(
                        "Spawning player "
                            + playerConfig.Name
                            + " with spawn id "
                            + playerConfig.SpawnId
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
                        _ => BotSkill.Hard
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

        return true;
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
            _deferredMatchSettings is MatchSettingsT matchSettings
            && _connectionReadies >= _expectedConnections
            && _needsSpawnCars
        )
        {
            bool spawned = SpawnCars(matchSettings);
            if (!spawned)
                return;

            _matchSettings = matchSettings;
            _deferredMatchSettings = null;
        }
    }
}
