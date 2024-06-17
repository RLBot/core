using System.Threading.Channels;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotSecret.Controller;
using RLBotSecret.Conversion;
using RLBotSecret.Models.Message;
using RLBotSecret.State;
using RLBotSecret.TCP;
using GameStateType = RLBotSecret.Models.Message.GameStateType;
using RLBotSecret.Models.Command;

namespace RLBotCS.Server
{
    internal class BridgeHandler(
        ChannelWriter<IServerMessage> writer,
        ChannelReader<IBridgeMessage> reader,
        TcpMessenger messenger
    )
    {
        // TODO: Use System.Threading.Lock when upgrading to C# 13
        private readonly object _gameStateLock = new();
        private GameState _gameState = new();

        public PlayerMapping PlayerMapping
        {
            get
            {
                lock (_gameStateLock)
                    return _gameState.PlayerMapping;
            }
        }

        private readonly MatchCommandSender _matchCommandSender = new(messenger);
        private readonly PlayerInputSender _playerInputSender = new(messenger);

        private bool _gotFirstMessage = false;
        private bool _matchHasStarted = false;
        private bool _queuedMatchCommands = false;
        private bool _delayMatchCommandSend = false;

        public void QueueConsoleCommand(string command)
        {
            _queuedMatchCommands = true;
            _matchCommandSender.AddConsoleCommand(command);
        }

        public ushort QueueSpawnCommand(string name, int team, BotSkill skill, Loadout loadout)
        {
            _queuedMatchCommands = true;
            return _matchCommandSender.AddBotSpawnCommand(name, team, skill, loadout);
        }

        public void SendPlayerInput(RLBotSecret.Models.Control.PlayerInput playerInput)
        {
            _playerInputSender.SendPlayerInput(playerInput);
        }

        public void SpawnMap(MatchSettingsT matchSettings)
        {
            _matchHasStarted = false;
            _delayMatchCommandSend = true;

            string loadMapCommand = FlatToCommand.MakeOpenCommand(matchSettings);
            Console.WriteLine("Core is about to start match with command: " + loadMapCommand);

            _matchCommandSender.AddConsoleCommand(loadMapCommand);
            _matchCommandSender.Send();
        }

        public void AddPendingSpawn(SpawnTracker spawnTracker)
        {
            lock (_gameStateLock)
                _gameState.PlayerMapping.AddPendingSpawn(spawnTracker);
        }

        private async Task HandleIncomingMessages()
        {
            await foreach (IBridgeMessage message in reader.ReadAllAsync())
                message.HandleMessage(this);
        }

        private async Task HandleServer()
        {
            await messenger.WaitForConnectionAsync();

            await foreach (var messageClump in messenger.ReadAllAsync())
            {
                if (!_gotFirstMessage)
                {
                    _gotFirstMessage = true;
                    writer.TryWrite(new StartCommunication());
                }

                lock (_gameStateLock)
                    _gameState = MessageHandler.CreateUpdatedState(messageClump, _gameState);

                var matchStarted = MessageHandler.ReceivedMatchInfo(messageClump);
                if (matchStarted)
                {
                    _matchHasStarted = true;
                    writer.TryWrite(new MapSpawned());
                }

                lock (_gameStateLock)
                {
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
                }

                writer.TryWrite(new DistributeGameState(_gameState));
            }
        }

        public void BlockingRun()
        {
            Task messagesTask = Task.Run(HandleIncomingMessages);
            Task serverTask = Task.Run(HandleServer);

            Task.WhenAny(messagesTask, serverTask).Wait();
        }

        public void Cleanup()
        {
            writer.TryComplete();
            messenger.Dispose();
        }
    }
}