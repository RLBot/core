using rlbot.flat;
using RLBotCS.Conversion;
using RLBotSecret.Controller;
using RLBotSecret.TCP;

namespace RLBotCS.GameControl
{
    internal class MatchStarter
    {
        private MatchCommandSender _matchCommandSender;
        private Mutex _commandSenderSync;
        private bool communicationStarted = false;
        private int _gamePort;

        private MatchSettingsT? _deferredMatchSettings;
        private MatchSettingsT? _matchSettings;

        private bool _hasEverLoadedMap = false;
        private bool _needsSpawnBots = false;

        public bool matchEnded = false;

        public MatchStarter(TcpMessenger tcpMessenger, Mutex tcpSync, int gamePort)
        {
            _matchCommandSender = new MatchCommandSender(tcpMessenger);
            _commandSenderSync = tcpSync;
            _gamePort = gamePort;
        }

        public void StartCommunication()
        {
            communicationStarted = true;
            MakeMatch(_deferredMatchSettings);
        }

        public void StartMatch(MatchSettingsT matchSettings)
        {
            if (!MatchManagement.Launcher.IsRocketLeagueRunning())
            {
                communicationStarted = false;
                MatchManagement.Launcher.LaunchRocketLeague(matchSettings.Launcher, _gamePort);
            }

            if (!communicationStarted)
            {
                // Defer the message
                _deferredMatchSettings = matchSettings;
                return;
            }

            MakeMatch(matchSettings);
        }

        public void MapSpawned()
        {
            Console.WriteLine("Match has started!");
            if (_needsSpawnBots)
            {
                // SpawnBots(_matchSettings);
                _needsSpawnBots = false;
            }
        }

        private void MakeMatch(MatchSettingsT? matchSettings)
        {
            if (matchSettings == null)
            {
                return;
            }

            Dictionary<string, int> playerNames = [];

            foreach (var playerConfig in matchSettings.PlayerConfigurations)
            {
                // De-duplicating similar names, Overwrites original value
                string playerName = playerConfig.Name;
                if (playerNames.TryGetValue(playerName, out int value))
                {
                    playerNames[playerName] = value++;
                    playerConfig.Name = playerName + $" ({value})";
                }
                else
                {
                    playerNames[playerName] = 0;
                    playerConfig.Name = playerName;
                }

                playerConfig.SpawnId = playerConfig.Name.GetHashCode();
            }

            if (matchSettings.AutoStartBots)
            {
                MatchManagement.Launcher.LaunchBots(matchSettings.PlayerConfigurations);
                MatchManagement.Launcher.LaunchScripts(matchSettings.ScriptConfigurations);
            }

            LoadMatch(matchSettings);
        }

        private void LoadMatch(MatchSettingsT matchSettings)
        {
            if (matchSettings.AutoSaveReplay)
            {
                _matchCommandSender.AddConsoleCommand(FlatToCommand.MakeAutoSaveReplayCommand());
            }

            var shouldSpawnNewMap = true;

            if (matchSettings.ExistingMatchBehavior == ExistingMatchBehavior.Continue_And_Spawn)
            {
                shouldSpawnNewMap = !_hasEverLoadedMap || matchEnded;
            }
            else if (matchSettings.ExistingMatchBehavior == ExistingMatchBehavior.Restart_If_Different)
            {
                shouldSpawnNewMap = IsDifferentFromLast(matchSettings);
            }

            _matchSettings = matchSettings;

            if (shouldSpawnNewMap)
            {
                _needsSpawnBots = true;
                _hasEverLoadedMap = true;

                // Load the map, then spawn the players AFTER the map loads.
                var loadMapCommand = FlatToCommand.MakeOpenCommand(matchSettings);
                Console.WriteLine("Core is about to start match with command: " + loadMapCommand);
                _matchCommandSender.AddConsoleCommand(loadMapCommand);

                _commandSenderSync.WaitOne();
                _matchCommandSender.Send();
                _commandSenderSync.ReleaseMutex();
            }
            else
            {
                // No need to load a new map, just spawn the players.
                // SpawnBots(matchSettings);
            }
        }

        public bool IsDifferentFromLast(MatchSettingsT matchSettings)
        {
            // don't consider rendering/state setting because that can be enable/disabled without restarting the match

            var lastMatchSettings = _matchSettings;
            if (lastMatchSettings == null)
            {
                return true;
            }

            if (lastMatchSettings.PlayerConfigurations.Count != matchSettings.PlayerConfigurations.Count)
            {
                return true;
            }

            for (var i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
            {
                var lastPlayerConfig = lastMatchSettings.PlayerConfigurations[i];
                var playerConfig = matchSettings.PlayerConfigurations[i];

                if (lastPlayerConfig.SpawnId != playerConfig.SpawnId)
                {
                    return true;
                }

                if (lastPlayerConfig.Team != playerConfig.Team)
                {
                    return true;
                }
            }

            if (lastMatchSettings.GameMode != matchSettings.GameMode)
            {
                return true;
            }

            if (lastMatchSettings.GameMapUpk != matchSettings.GameMapUpk)
            {
                return true;
            }

            if (lastMatchSettings.InstantStart != matchSettings.InstantStart)
            {
                return true;
            }

            var lastMutators = lastMatchSettings.MutatorSettings;
            var mutators = matchSettings.MutatorSettings;

            if (lastMutators.MatchLength != mutators.MatchLength)
            {
                return true;
            }

            if (lastMutators.MaxScore != mutators.MaxScore)
            {
                return true;
            }

            if (lastMutators.OvertimeOption != mutators.OvertimeOption)
            {
                return true;
            }

            if (lastMutators.SeriesLengthOption != mutators.SeriesLengthOption)
            {
                return true;
            }

            if (lastMutators.BallMaxSpeedOption != mutators.BallMaxSpeedOption)
            {
                return true;
            }

            if (lastMutators.BallTypeOption != mutators.BallTypeOption)
            {
                return true;
            }

            if (lastMutators.BallWeightOption != mutators.BallWeightOption)
            {
                return true;
            }

            if (lastMutators.BallSizeOption != mutators.BallSizeOption)
            {
                return true;
            }

            if (lastMutators.BallBouncinessOption != mutators.BallBouncinessOption)
            {
                return true;
            }

            if (lastMutators.BoostOption != mutators.BoostOption)
            {
                return true;
            }

            if (lastMutators.RumbleOption != mutators.RumbleOption)
            {
                return true;
            }

            if (lastMutators.BoostStrengthOption != mutators.BoostStrengthOption)
            {
                return true;
            }

            if (lastMutators.DemolishOption != mutators.DemolishOption)
            {
                return true;
            }

            if (lastMutators.RespawnTimeOption != mutators.RespawnTimeOption)
            {
                return true;
            }

            return false;
        }

        // private void SpawnBots(MatchSettingsT matchSettings)
        // {
        //     PlayerConfigurationT? humanConfig = null;
        //     Dictionary<string, int> playerNames = [];
        //     var humanIndex = -1;
        //     var indexOffset = 0;

        //     for (int i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
        //     {
        //         var playerConfig = matchSettings.PlayerConfigurations[i];

        //         var alreadySpawnedPlayer = _playerMapping
        //             .GetKnownPlayers()
        //             .FirstOrDefault((kp) => playerConfig.SpawnId == kp.SpawnId);
        //         if (alreadySpawnedPlayer != null)
        //         {
        //             // We've already spawned this player, don't duplicate them.
        //             continue;
        //         }

        //         if (playerConfig.Loadout is null)
        //         {
        //             playerConfig.Loadout = new PlayerLoadoutT();
        //         }

        //         if (playerConfig.Loadout.LoadoutPaint is null)
        //         {
        //             playerConfig.Loadout.LoadoutPaint = new LoadoutPaintT();
        //         }

        //         var loadout = FlatToModel.ToLoadout(
        //             playerConfig.Loadout,
        //             playerConfig.Team
        //         );

        //         switch (playerConfig.Variety.Type)
        //         {
        //             case PlayerClass.RLBot:

        //                 Console.WriteLine(
        //                     "Core is spawning player "
        //                         + playerConfig.Name
        //                         + " with spawn id "
        //                         + playerConfig.SpawnId
        //                 );

        //                 var rlbotSpawnCommandId = _matchCommandSender.AddBotSpawnCommand(
        //                     playerConfig.Name,
        //                     (int)playerConfig.Team,
        //                     BotSkill.Custom,
        //                     loadout
        //                 );

        //                 _playerMapping.AddPendingSpawn(
        //                     new SpawnTracker()
        //                     {
        //                         CommandId = rlbotSpawnCommandId,
        //                         SpawnId = playerConfig.SpawnId,
        //                         DesiredPlayerIndex = (uint)(i - indexOffset),
        //                         IsCustomBot = true,
        //                     }
        //                 );
        //                 break;
        //             case PlayerClass.Psyonix:
        //                 var skill = playerConfig.Variety.AsPsyonix().BotSkill;
        //                 var skillEnum = BotSkill.Hard;
        //                 if (skill < 0.5)
        //                 {
        //                     skillEnum = BotSkill.Easy;
        //                 }
        //                 else if (skill < 1)
        //                 {
        //                     skillEnum = BotSkill.Medium;
        //                 }

        //                 var psySpawnCommandId = _matchCommandSender.AddBotSpawnCommand(
        //                     playerConfig.Name,
        //                     (int)playerConfig.Team,
        //                     skillEnum,
        //                     loadout
        //                 );

        //                 _playerMapping.AddPendingSpawn(
        //                     new SpawnTracker()
        //                     {
        //                         CommandId = psySpawnCommandId,
        //                         SpawnId = playerConfig.SpawnId,
        //                         DesiredPlayerIndex = (uint)(i - indexOffset),
        //                         IsCustomBot = false
        //                     }
        //                 );
        //                 break;
        //             case PlayerClass.Human:
        //                 if (humanConfig != null)
        //                 {
        //                     // We can't spawn this human player,
        //                     // so we need to -1 for ever index after this
        //                     // to properly set the desired player indicies
        //                     indexOffset++;
        //                     Console.WriteLine(
        //                         "Warning: Multiple human players requested. RLBot only supports spawning max one human per match."
        //                     );
        //                 }
        //                 else
        //                 {
        //                     humanConfig = playerConfig;
        //                     // indexOffset can only ever be 0 here
        //                     humanIndex = i;
        //                 }

        //                 break;
        //         }
        //     }

        //     if (humanConfig != null)
        //     {
        //         _commandSenderSync.WaitOne();
        //         _matchCommandSender.Send();
        //         _commandSenderSync.ReleaseMutex();

        //         // For some reason if we send this command to early,
        //         // the game will only half-spawn us
        //         _matchCommandSender.AddConsoleCommand("ChangeTeam " + humanConfig.Team);

        //         _playerMapping.AddPendingSpawn(
        //             new SpawnTracker()
        //             {
        //                 CommandId = 0,
        //                 SpawnId = humanConfig.SpawnId,
        //                 DesiredPlayerIndex = (uint)humanIndex,
        //                 IsCustomBot = false,
        //             }
        //         );
        //     }
        //     else
        //     {
        //         // If no human was requested for the match,
        //         // then make the human spectate so we can start the match
        //         _matchCommandSender.AddConsoleCommand("spectate");
        //     }

        //     _commandSenderSync.WaitOne();
        //     _matchCommandSender.Send();
        //     _commandSenderSync.ReleaseMutex();
        // }
    }
}
