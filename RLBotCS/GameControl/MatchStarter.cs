using rlbot.flat;
using RLBotCS.Conversion;
using RLBotSecret.Controller;
using RLBotSecret.Conversion;
using RLBotSecret.State;
using RLBotSecret.Models.Message;
using RLBotSecret.TCP;
using RLBotSecret.Types;
using GameStateType = RLBotSecret.Models.Message.GameStateType;

namespace RLBotCS.GameControl
{
    internal class MatchStarter
    {
        private GameState _gameState;
        private PlayerMapping _playerMapping;
        private MatchCommandSender _matchCommandSender;
        private (MatchSettingsT, TypedPayload)? _lastMatchMessage;
        private (MatchSettingsT, TypedPayload)? _deferredMatchMessage;
        private MatchLength _matchLength = rlbot.flat.MatchLength.Five_Minutes;
        private float _respawnTime = 3;
        private bool _needsSpawnBots = true;
        private bool _hasEverLoadedMap = false;
        private bool _isStateSettingEnabled = true;
        private bool _isRenderingEnabled = true;
        private int _gamePort;

        public MatchStarter(TcpMessenger tcpMessenger, GameState gameState, int gamePort)
        {
            this._gameState = gameState;
            _playerMapping = gameState.PlayerMapping;
            _matchCommandSender = new MatchCommandSender(tcpMessenger);
            this._gamePort = gamePort;
        }

        public void EndMatch()
        {
            _matchCommandSender.AddMatchEndCommand();
            _matchCommandSender.Send();
        }

        public void SetDesiredGameState(DesiredGameStateT desiredGameState)
        {
            if (desiredGameState.GameInfoState is DesiredGameInfoStateT gameState)
            {
                if (gameState.WorldGravityZ is FloatT worldGravityZ)
                {
                    _matchCommandSender.AddConsoleCommand(FlatToCommand.MakeGravityCommand(worldGravityZ.Val));
                }

                if (gameState.GameSpeed is FloatT gameSpeed)
                {
                    _matchCommandSender.AddConsoleCommand(FlatToCommand.MakeGameSpeedCommand(gameSpeed.Val));
                }

                if (gameState.Paused is BoolT paused)
                {
                    _matchCommandSender.AddSetPausedCommand(paused.Val);
                }

                if (gameState.EndMatch is BoolT endMatch && endMatch.Val)
                {
                    _matchCommandSender.AddMatchEndCommand();
                }
            }

            for (var i = 0; i < desiredGameState.CarStates.Count; i++)
            {
                if (
                    desiredGameState.CarStates[i] is DesiredCarStateT carState
                    && _playerMapping.ActorIdFromPlayerIndex((uint)i) is ushort actorId
                )
                {
                    if (carState.Physics is DesiredPhysicsT physics)
                    {
                        var defaultPhysics = this._gameState.GameCars[(uint)i].Physics;
                        _matchCommandSender.AddSetPhysicsCommand(
                            actorId,
                            FlatToModel.DesiredToPhysics(physics, defaultPhysics)
                        );
                    }

                    if (carState.BoostAmount is FloatT boostAmount)
                    {
                        _matchCommandSender.AddSetBoostCommand(actorId, (int)boostAmount.Val);
                    }
                }
            }

            if (desiredGameState.BallState is DesiredBallStateT ballState)
            {
                if (ballState.Physics is DesiredPhysicsT physics)
                {
                    _matchCommandSender.AddSetPhysicsCommand(
                        this._gameState.Ball.ActorId,
                        FlatToModel.DesiredToPhysics(physics, this._gameState.Ball.Physics)
                    );
                }
            }

            _matchCommandSender.Send();
        }

        private void LoadMatch(MatchSettingsT matchSettings, TypedPayload originalMessage)
        {
            if (matchSettings.MutatorSettings is MutatorSettingsT mutatorSettings)
            {
                _matchLength = mutatorSettings.MatchLength;
                _matchCommandSender.AddConsoleCommand(
                    FlatToCommand.MakeGravityCommandFromOption(mutatorSettings.GravityOption)
                );
                _matchCommandSender.AddConsoleCommand(
                    FlatToCommand.MakeGameSpeedCommandFromOption(mutatorSettings.GameSpeedOption)
                );

                if (mutatorSettings.RespawnTimeOption is RespawnTimeOption respawnTimeOption)
                {
                    if (respawnTimeOption == RespawnTimeOption.Two_Seconds)
                    {
                        _respawnTime = 2;
                    }
                    else if (respawnTimeOption == RespawnTimeOption.One_Seconds)
                    {
                        _respawnTime = 1;
                    }
                    else
                    {
                        _respawnTime = 3;
                    }
                }
            }

            _isStateSettingEnabled = matchSettings.EnableStateSetting;
            _isRenderingEnabled = matchSettings.EnableRendering;

            if (matchSettings.AutoSaveReplay)
            {
                _matchCommandSender.AddConsoleCommand(FlatToCommand.MakeAutoSaveReplayCommand());
            }

            var shouldSpawnNewMap = true;

            if (matchSettings.ExistingMatchBehavior == ExistingMatchBehavior.Continue_And_Spawn)
            {
                shouldSpawnNewMap = !_hasEverLoadedMap || _gameState.GameStateType == GameStateType.Ended;
            }
            else if (matchSettings.ExistingMatchBehavior == ExistingMatchBehavior.Restart_If_Different)
            {
                shouldSpawnNewMap = IsDifferentFromLast(matchSettings);
            }

            _lastMatchMessage = (matchSettings, originalMessage);

            if (shouldSpawnNewMap)
            {
                _needsSpawnBots = true;
                _hasEverLoadedMap = true;

                // Load the map, then spawn the players AFTER the map loads.
                var loadMapCommand = FlatToCommand.MakeOpenCommand(matchSettings);
                Console.WriteLine("Core is about to start match with command: " + loadMapCommand);
                _matchCommandSender.AddConsoleCommand(loadMapCommand);
                _matchCommandSender.Send();
            }
            else
            {
                // No need to load a new map, just spawn the players.
                SpawnBots(matchSettings);
            }
        }

        public void LoadDeferredMatch()
        {
            if (_deferredMatchMessage is (MatchSettingsT, TypedPayload) matchMessage)
            {
                LoadMatch(matchMessage.Item1, matchMessage.Item2);
                _deferredMatchMessage = null;
            }
        }

        public void HandleMatchSettings(
            MatchSettingsT matchSettings,
            TypedPayload originalMessage,
            bool deferLoadMap
        )
        {
            if (!MatchManagement.Launcher.IsRocketLeagueRunning())
            {
                MatchManagement.Launcher.LaunchRocketLeague(matchSettings.Launcher, _gamePort);
            }

            Dictionary<string, int> playerNames = [];

            foreach (var playerConfig in matchSettings.PlayerConfigurations)
            {
                // De-duplicating similar names, Overwrites original value
                string playerName = playerConfig.Name;
                if (playerNames.TryGetValue(playerName, out int value))
                {
                    playerNames[playerName] = value++;
                    playerConfig.Name = playerName + $" ({value})"; // " (x)"
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

            if (deferLoadMap)
            {
                _deferredMatchMessage = (matchSettings, originalMessage);
            }
            else
            {
                LoadMatch(matchSettings, originalMessage);
            }
        }

        public bool IsDifferentFromLast(MatchSettingsT matchSettings)
        {
            // don't consider rendering/state setting because that can be enable/disabled without restarting the match

            var lastMatchSettings = _lastMatchMessage?.Item1;
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

        public void ApplyMessageBundle(MessageBundle messageBundle)
        {
            if (_needsSpawnBots && _lastMatchMessage?.Item1 is MatchSettingsT matchSettings)
            {
                if (messageBundle.Messages.Any(message => message is MatchInfo))
                {
                    SpawnBots(matchSettings);
                    _needsSpawnBots = false;
                }
            }
        }

        private void SpawnBots(MatchSettingsT matchSettings)
        {
            PlayerConfigurationT? humanConfig = null;
            Dictionary<string, int> playerNames = [];
            var humanIndex = -1;
            var indexOffset = 0;

            for (int i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
            {
                var playerConfig = matchSettings.PlayerConfigurations[i];

                var alreadySpawnedPlayer = _playerMapping
                    .GetKnownPlayers()
                    .FirstOrDefault((kp) => playerConfig.SpawnId == kp.SpawnId);
                if (alreadySpawnedPlayer != null)
                {
                    // We've already spawned this player, don't duplicate them.
                    continue;
                }

                if (playerConfig.Loadout is null)
                {
                    playerConfig.Loadout = new PlayerLoadoutT();
                }

                if (playerConfig.Loadout.LoadoutPaint is null)
                {
                    playerConfig.Loadout.LoadoutPaint = new LoadoutPaintT();
                }

                var loadout = FlatToModel.ToLoadout(
                    playerConfig.Loadout,
                    playerConfig.Team
                );

                switch (playerConfig.Variety.Type)
                {
                    case PlayerClass.RLBot:

                        Console.WriteLine(
                            "Core is spawning player "
                                + playerConfig.Name
                                + " with spawn id "
                                + playerConfig.SpawnId
                        );

                        var rlbotSpawnCommandId = _matchCommandSender.AddBotSpawnCommand(
                            playerConfig.Name,
                            (int)playerConfig.Team,
                            BotSkill.Custom,
                            loadout
                        );

                        _playerMapping.AddPendingSpawn(
                            new SpawnTracker()
                            {
                                CommandId = rlbotSpawnCommandId,
                                SpawnId = playerConfig.SpawnId,
                                DesiredPlayerIndex = (uint)(i - indexOffset),
                                IsCustomBot = true,
                            }
                        );
                        break;
                    case PlayerClass.Psyonix:
                        var skill = playerConfig.Variety.AsPsyonix().BotSkill;
                        var skillEnum = BotSkill.Hard;
                        if (skill < 0.5)
                        {
                            skillEnum = BotSkill.Easy;
                        }
                        else if (skill < 1)
                        {
                            skillEnum = BotSkill.Medium;
                        }

                        var psySpawnCommandId = _matchCommandSender.AddBotSpawnCommand(
                            playerConfig.Name,
                            (int)playerConfig.Team,
                            skillEnum,
                            loadout
                        );

                        _playerMapping.AddPendingSpawn(
                            new SpawnTracker()
                            {
                                CommandId = psySpawnCommandId,
                                SpawnId = playerConfig.SpawnId,
                                DesiredPlayerIndex = (uint)(i - indexOffset),
                                IsCustomBot = false
                            }
                        );
                        break;
                    case PlayerClass.Human:
                        if (humanConfig != null)
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
                            humanConfig = playerConfig;
                            // indexOffset can only ever be 0 here
                            humanIndex = i;
                        }

                        break;
                }
            }

            if (humanConfig != null)
            {
                _matchCommandSender.Send();

                // For some reason if we send this command to early,
                // the game will only half-spawn us
                _matchCommandSender.AddConsoleCommand("ChangeTeam " + humanConfig.Team);

                _playerMapping.AddPendingSpawn(
                    new SpawnTracker()
                    {
                        CommandId = 0,
                        SpawnId = humanConfig.SpawnId,
                        DesiredPlayerIndex = (uint)humanIndex,
                        IsCustomBot = false,
                    }
                );
            }
            else
            {
                // If no human was requested for the match,
                // then make the human spectate so we can start the match
                _matchCommandSender.AddConsoleCommand("spectate");
            }

            _matchCommandSender.Send();
        }

        public TypedPayload? GetMatchSettings()
        {
            return _lastMatchMessage?.Item2;
        }

        public MatchLength MatchLength()
        {
            return _matchLength;
        }

        public float RespawnTime()
        {
            return _respawnTime;
        }

        internal bool IsStateSettingEnabled()
        {
            return _isStateSettingEnabled;
        }

        internal bool IsRenderingEnabled()
        {
            return _isRenderingEnabled;
        }
    }
}
