using System.Threading.Channels;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.Server;
using Bridge.Models.Message;
using ConsoleCommand = RLBotCS.Server.ConsoleCommand;

namespace RLBotCS.ManagerTools
{
    internal class MatchStarter
    {
        private ChannelWriter<IBridgeMessage> _bridge;
        private bool communicationStarted = false;
        private int _gamePort;

        private MatchSettingsT? _deferredMatchSettings;
        private MatchSettingsT? _matchSettings;

        private bool _hasEverLoadedMap = false;
        private bool _needsSpawnBots = false;

        public bool matchEnded = false;

        public MatchStarter(ChannelWriter<IBridgeMessage> bridge, int gamePort)
        {
            _bridge = bridge;
            _gamePort = gamePort;
        }

        public MatchSettingsT? GetMatchSettings()
        {
            return _deferredMatchSettings ?? _matchSettings;
        }

        public void NullMatchSettings()
        {
            _matchSettings = null;
            _deferredMatchSettings = null;
        }

        public void StartCommunication()
        {
            communicationStarted = true;
            MakeMatch(_deferredMatchSettings);
            _deferredMatchSettings = null;
        }

        public void StartMatch(MatchSettingsT matchSettings)
        {
            if (!ManagerTools.Launching.IsRocketLeagueRunning())
            {
                communicationStarted = false;
                ManagerTools.Launching.LaunchRocketLeague(matchSettings.Launcher, _gamePort);
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
            if (_needsSpawnBots && _matchSettings != null)
            {
                SpawnCars(_matchSettings);
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
                    playerNames[playerName] = ++value;
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
                ManagerTools.Launching.LaunchBots(matchSettings.PlayerConfigurations);
                ManagerTools.Launching.LaunchScripts(matchSettings.ScriptConfigurations);
            }

            LoadMatch(matchSettings);
        }

        private void LoadMatch(MatchSettingsT matchSettings)
        {
            if (matchSettings.AutoSaveReplay)
            {
                _bridge.TryWrite(new ConsoleCommand(FlatToCommand.MakeAutoSaveReplayCommand()));
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

                _bridge.TryWrite(new SpawnMap(matchSettings));
            }
            else
            {
                // No need to load a new map, just spawn the players.
                SpawnCars(matchSettings);
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

        private void SpawnCars(MatchSettingsT matchSettings)
        {
            bool hasHuman = false;
            int indexOffset = 0;

            for (int i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
            {
                var playerConfig = matchSettings.PlayerConfigurations[i];

                switch (playerConfig.Variety.Type)
                {
                    case PlayerClass.RLBot:
                        Console.WriteLine(
                            "Core is spawning player "
                                + playerConfig.Name
                                + " with spawn id "
                                + playerConfig.SpawnId
                        );

                        _bridge.TryWrite(
                            new SpawnBot(playerConfig, BotSkill.Custom, (uint)(i - indexOffset), true)
                        );

                        break;
                    case PlayerClass.Psyonix:
                        var skill = playerConfig.Variety.AsPsyonix().BotSkill;
                        var skillEnum = BotSkill.Hard;
                        if (skill < 0)
                        {
                            skillEnum = BotSkill.Intro;
                        }
                        else if (skill < 0.5)
                        {
                            skillEnum = BotSkill.Easy;
                        }
                        else if (skill < 1)
                        {
                            skillEnum = BotSkill.Medium;
                        }

                        _bridge.TryWrite(new SpawnBot(playerConfig, skillEnum, (uint)(i - indexOffset), false));

                        break;
                    case PlayerClass.Human:
                        if (hasHuman)
                        {
                            // We can't spawn this human player,
                            // so we need to -1 for ever index after this
                            // to properly set the desired player indicies
                            indexOffset++;
                            Console.WriteLine(
                                "Warning: Multiple human players requested. RLBot only supports spawning max one human per match."
                            );
                        }
                        else
                        {
                            hasHuman = true;
                            // indexOffset can only ever be 0 here
                            _bridge.TryWrite(new SpawnHuman(playerConfig, (uint)i));
                        }

                        break;
                }
            }

            if (!hasHuman)
            {
                // If no human was requested for the match,
                // then make the human spectate so we can start the match
                _bridge.TryWrite(new ConsoleCommand("spectate"));
            }
        }
    }
}
