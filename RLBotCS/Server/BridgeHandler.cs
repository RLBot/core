using System.Threading.Channels;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotSecret.Controller;
using RLBotSecret.Conversion;
using RLBotSecret.Models.Message;
using RLBotSecret.State;
using RLBotSecret.TCP;
using GameStateType = RLBotSecret.Models.Message.GameStateType;

namespace RLBotCS.Server
{
    internal enum BridgeMessageType
    {
        SpawnMap,
        ConsoleCommand,
        SpawnBot,
        SpawnHuman,
        PlayerInput,
        Stop,
    }

    internal struct BridgeMessage
    {
        private BridgeMessageType _type;

        private MatchSettingsT _matchSettings;
        private string _consoleCommand;
        private (PlayerConfigurationT, BotSkill, uint, bool) _spawnBot;
        private (PlayerConfigurationT, uint) _spawnHuman;
        private PlayerInput playerInput;

        public static BridgeMessage Stop()
        {
            return new BridgeMessage { _type = BridgeMessageType.Stop };
        }

        public static BridgeMessage SpawnMap(MatchSettingsT matchSettings)
        {
            return new BridgeMessage { _type = BridgeMessageType.SpawnMap, _matchSettings = matchSettings };
        }

        public static BridgeMessage ConsoleCommand(string consoleCommand)
        {
            return new BridgeMessage
            {
                _type = BridgeMessageType.ConsoleCommand,
                _consoleCommand = consoleCommand
            };
        }

        public static BridgeMessage SpawnBot(
            PlayerConfigurationT playerConfig,
            BotSkill skill,
            uint desiredIndex,
            bool isCustomBot
        )
        {
            return new BridgeMessage
            {
                _type = BridgeMessageType.SpawnBot,
                _spawnBot = (playerConfig, skill, desiredIndex, isCustomBot)
            };
        }

        public static BridgeMessage SpawnHuman(PlayerConfigurationT playerConfig, uint desiredIndex)
        {
            return new BridgeMessage
            {
                _type = BridgeMessageType.SpawnHuman,
                _spawnHuman = (playerConfig, desiredIndex)
            };
        }

        public static BridgeMessage PlayerInput(PlayerInput playerInput)
        {
            return new BridgeMessage { _type = BridgeMessageType.PlayerInput, playerInput = playerInput };
        }

        public BridgeMessageType Type()
        {
            return _type;
        }

        public MatchSettingsT MatchSettings()
        {
            return _matchSettings;
        }

        public string ConsoleCommand()
        {
            return _consoleCommand;
        }

        public (PlayerConfigurationT, BotSkill, uint, bool) BotSpawn()
        {
            return _spawnBot;
        }

        public (PlayerConfigurationT, uint) SpawnHuman()
        {
            return _spawnHuman;
        }

        public PlayerInput PlayerInput()
        {
            return playerInput;
        }
    }

    internal class BridgeHandler
    {
        private ChannelReader<BridgeMessage> _incomingMessages;
        private ChannelWriter<ServerMessage> _rlbotServer;
        private GameState _gameState = new GameState();

        private TcpMessenger _messenger;
        private MatchCommandSender _matchCommandSender;
        private PlayerInputSender _playerInputSender;

        private bool _gotFirstMessage = false;
        private bool _matchHasStarted = false;
        private bool _queuedMatchCommands = false;
        private bool _delayMatchCommandSend = false;

        public BridgeHandler(
            ChannelWriter<ServerMessage> rlbotServer,
            ChannelReader<BridgeMessage> incomingMessages,
            TcpMessenger messenger
        )
        {
            _rlbotServer = rlbotServer;
            _incomingMessages = incomingMessages;

            _messenger = messenger;
            _matchCommandSender = new MatchCommandSender(messenger);
            _playerInputSender = new PlayerInputSender(messenger);
        }

        private void SpawnMap(MatchSettingsT matchSettings)
        {
            var loadMapCommand = FlatToCommand.MakeOpenCommand(matchSettings);
            Console.WriteLine("Core is about to start match with command: " + loadMapCommand);

            _matchCommandSender.AddConsoleCommand(loadMapCommand);
            _matchCommandSender.Send();
        }

        private void SpawnHuman(PlayerConfigurationT humanConfig, uint desiredIndex)
        {
            _matchCommandSender.AddConsoleCommand("ChangeTeam " + humanConfig.Team);

            _gameState.PlayerMapping.AddPendingSpawn(
                new SpawnTracker()
                {
                    CommandId = 0,
                    SpawnId = humanConfig.SpawnId,
                    DesiredPlayerIndex = (uint)desiredIndex,
                    IsCustomBot = false,
                }
            );
        }

        private void SpawnBot(
            PlayerConfigurationT playerConfig,
            BotSkill skill,
            uint desiredIndex,
            bool isCustomBot
        )
        {
            var alreadySpawnedPlayer = _gameState
                .PlayerMapping.GetKnownPlayers()
                .FirstOrDefault((kp) => playerConfig.SpawnId == kp.SpawnId);
            if (alreadySpawnedPlayer != null)
            {
                // We've already spawned this player, don't duplicate them.
                return;
            }

            if (playerConfig.Loadout is null)
            {
                playerConfig.Loadout = new PlayerLoadoutT();
            }

            if (playerConfig.Loadout.LoadoutPaint is null)
            {
                playerConfig.Loadout.LoadoutPaint = new LoadoutPaintT();
            }

            var loadout = FlatToModel.ToLoadout(playerConfig.Loadout, playerConfig.Team);

            var commandId = _matchCommandSender.AddBotSpawnCommand(
                playerConfig.Name,
                (int)playerConfig.Team,
                skill,
                loadout
            );

            _gameState.PlayerMapping.AddPendingSpawn(
                new SpawnTracker()
                {
                    CommandId = commandId,
                    SpawnId = playerConfig.SpawnId,
                    DesiredPlayerIndex = desiredIndex,
                    IsCustomBot = isCustomBot,
                }
            );
        }

        private async Task HandleIncommingMessages()
        {
            await foreach (BridgeMessage message in _incomingMessages.ReadAllAsync())
            {
                switch (message.Type())
                {
                    case BridgeMessageType.Stop:
                        return;
                    case BridgeMessageType.SpawnMap:
                        _matchHasStarted = false;
                        _delayMatchCommandSend = true;

                        SpawnMap(message.MatchSettings());
                        break;
                    case BridgeMessageType.ConsoleCommand:
                        _queuedMatchCommands = true;

                        _matchCommandSender.AddConsoleCommand(message.ConsoleCommand());
                        break;
                    case BridgeMessageType.SpawnBot:
                        _queuedMatchCommands = true;

                        var (playerConfig, skill, desiredIndex, isCustomBot) = message.BotSpawn();
                        SpawnBot(playerConfig, skill, desiredIndex, isCustomBot);
                        break;
                    case BridgeMessageType.SpawnHuman:
                        _queuedMatchCommands = true;

                        var (humanConfig, humanIndex) = message.SpawnHuman();
                        SpawnHuman(humanConfig, humanIndex);
                        break;
                    case BridgeMessageType.PlayerInput:
                        var playerInputMsg = message.PlayerInput();
                        var carInput = FlatToModel.ToCarInput(playerInputMsg.ControllerState.Value);
                        var actorId = _gameState.PlayerMapping.ActorIdFromPlayerIndex(playerInputMsg.PlayerIndex);

                        if (actorId.HasValue)
                        {
                            var playerInput = new RLBotSecret.Models.Control.PlayerInput()
                            {
                                ActorId = actorId.Value,
                                CarInput = carInput
                            };
                            _playerInputSender.SendPlayerInput(playerInput);
                        }
                        else
                        {
                            Console.WriteLine(
                                "Core got input from unknown player index {0}",
                                playerInputMsg.PlayerIndex
                            );
                        }
                        break;
                }
            }
        }

        private async Task HandleServer()
        {
            await _messenger.WaitForConnectionAsync();

            await foreach (var messageClump in _messenger.ReadAllAsync())
            {
                if (!_gotFirstMessage)
                {
                    _gotFirstMessage = true;
                    _rlbotServer.TryWrite(ServerMessage.StartCommunication());
                }

                _gameState = MessageHandler.CreateUpdatedState(messageClump, _gameState);

                var matchStarted = MessageHandler.ReceivedMatchInfo(messageClump);
                if (matchStarted)
                {
                    _matchHasStarted = true;
                    _rlbotServer.TryWrite(ServerMessage.MapSpawned());
                }

                if (
                    _delayMatchCommandSend
                    && _queuedMatchCommands
                    && _matchHasStarted
                    && _gameState.GameStateType == GameStateType.Paused
                )
                {
                    // if we send the commands before the map has spawned, nothing will happen
                    _delayMatchCommandSend = false;
                    _queuedMatchCommands = false;

                    _matchCommandSender.Send();
                }

                _rlbotServer.TryWrite(ServerMessage.DistributeGameState(_gameState));
            }
        }

        public void BlockingRun()
        {
            Task incommingMessagesTask = Task.Run(HandleIncommingMessages);
            Task serverTask = Task.Run(HandleServer);

            Task.WhenAny(incommingMessagesTask, serverTask).Wait();
        }

        public void Cleanup()
        {
            _rlbotServer.TryComplete();
            _messenger.Dispose();
        }
    }
}
