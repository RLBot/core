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
        Stop,
    }

    internal struct BridgeMessage
    {
        private BridgeMessageType _type;

        private MatchSettingsT _matchSettings;
        private string _consoleCommand;
        private (PlayerConfigurationT, BotSkill, uint, bool) _spawnBot;
        private (PlayerConfigurationT, uint) _spawnHuman;

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
    }

    internal class BridgeHandler
    {
        private ChannelReader<BridgeMessage> _incomingMessages;
        private ChannelWriter<ServerMessage> _rlbotServer;
        private GameState _gameState = new GameState();

        private TcpMessenger _messenger;
        private MatchCommandSender _matchCommandSender;
        private Mutex _messengerSync;

        private bool _gotFirstMessage = false;

        public BridgeHandler(
            ChannelWriter<ServerMessage> rlbotServer,
            ChannelReader<BridgeMessage> incomingMessages,
            TcpMessenger messenger,
            Mutex messengerSync
        )
        {
            _rlbotServer = rlbotServer;
            _incomingMessages = incomingMessages;

            _messenger = messenger;
            _matchCommandSender = new MatchCommandSender(messenger);
            _messengerSync = messengerSync;
        }

        private void SpawnMap(MatchSettingsT matchSettings)
        {
            var loadMapCommand = FlatToCommand.MakeOpenCommand(matchSettings);
            Console.WriteLine("Core is about to start match with command: " + loadMapCommand);

            _matchCommandSender.AddConsoleCommand(loadMapCommand);

            _messengerSync.WaitOne();
            _matchCommandSender.Send();
            _messengerSync.ReleaseMutex();
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

        public void BlockingRun()
        {
            SpinWait spinWait = new SpinWait();
            ArraySegment<byte> messageClump = null;

            _messengerSync.WaitOne();
            _messenger.WaitForConnection();
            _messengerSync.ReleaseMutex();

            bool matchHasStarted = false;
            bool queuedMatchCommands = false;
            bool delayMatchCommandSend = false;

            while (true)
            {
                bool handledSomething = false;

                while (_incomingMessages.TryRead(out BridgeMessage message))
                {
                    handledSomething = true;

                    switch (message.Type())
                    {
                        case BridgeMessageType.Stop:
                            return;
                        case BridgeMessageType.SpawnMap:
                            matchHasStarted = false;
                            delayMatchCommandSend = true;

                            SpawnMap(message.MatchSettings());
                            break;
                        case BridgeMessageType.ConsoleCommand:
                            queuedMatchCommands = true;

                            _matchCommandSender.AddConsoleCommand(message.ConsoleCommand());
                            break;
                        case BridgeMessageType.SpawnBot:
                            queuedMatchCommands = true;

                            var (playerConfig, skill, desiredIndex, isCustomBot) = message.BotSpawn();
                            SpawnBot(playerConfig, skill, desiredIndex, isCustomBot);
                            break;
                        case BridgeMessageType.SpawnHuman:
                            queuedMatchCommands = true;

                            var (humanConfig, humanIndex) = message.SpawnHuman();
                            SpawnHuman(humanConfig, humanIndex);
                            break;
                    }
                }

                // if the channel has closed and somehow there was no Stop command,
                // exit anyways to be safe
                if (_incomingMessages.Completion.IsCompleted)
                {
                    return;
                }

                _messengerSync.WaitOne();

                if (!delayMatchCommandSend && queuedMatchCommands)
                {
                    queuedMatchCommands = false;

                    _matchCommandSender.Send();
                }

                if (_messenger.TryRead(out messageClump))
                {
                    handledSomething = true;

                    if (!_gotFirstMessage)
                    {
                        _gotFirstMessage = true;
                        _rlbotServer.TryWrite(ServerMessage.StartCommunication());
                    }

                    _gameState = MessageHandler.CreateUpdatedState(messageClump, _gameState);

                    var matchStarted = MessageHandler.ReceivedMatchInfo(messageClump);
                    if (matchStarted)
                    {
                        matchHasStarted = true;
                        _rlbotServer.TryWrite(ServerMessage.MapSpawned());
                    }

                    if (
                        delayMatchCommandSend
                        && queuedMatchCommands
                        && matchHasStarted
                        && _gameState.GameStateType == GameStateType.Paused
                    )
                    {
                        // if we send the commands before the map has spawned, nothing will happen
                        delayMatchCommandSend = false;
                        queuedMatchCommands = false;

                        _matchCommandSender.Send();
                    }

                    _rlbotServer.TryWrite(ServerMessage.DistributeGameState(_gameState));
                }
                _messengerSync.ReleaseMutex();

                if (!handledSomething)
                {
                    spinWait.SpinOnce();
                }
            }
        }

        public void Cleanup()
        {
            _rlbotServer.TryComplete();
            _messenger.Dispose();
        }
    }
}
