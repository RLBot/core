using System.Threading.Channels;
using RLBotSecret.Conversion;
using RLBotSecret.State;
using RLBotSecret.TCP;

namespace RLBotCS.Server
{
    internal enum BridgeMessageType
    {
        Stop,
    }

    internal struct BridgeMessage
    {
        private BridgeMessageType _type;

        public static BridgeMessage Stop()
        {
            return new BridgeMessage { _type = BridgeMessageType.Stop };
        }

        public BridgeMessageType Type()
        {
            return _type;
        }
    }

    internal class BridgeHandler
    {
        private ChannelReader<BridgeMessage> _incomingMessages;
        private ChannelWriter<ServerMessage> _rlbotServer;
        private GameState _gameState = new GameState();

        private TcpMessenger _messenger;
        private Mutex _messengerMutex;

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
            _messengerMutex = messengerSync;
        }

        public void BlockingRun()
        {
            SpinWait spinWait = new SpinWait();
            ArraySegment<byte> messageClump = null;

            _messengerMutex.WaitOne();
            _messenger.WaitForConnection();
            _messengerMutex.ReleaseMutex();

            while (true)
            {
                bool handledSomething = false;

                if (_incomingMessages.Completion.IsCompleted)
                {
                    return;
                }

                while (_incomingMessages.TryRead(out BridgeMessage message))
                {
                    handledSomething = true;

                    switch (message.Type())
                    {
                        case BridgeMessageType.Stop:
                            return;
                    }
                }

                _messengerMutex.WaitOne();
                if (_messenger.TryRead(out messageClump))
                {
                    handledSomething = true;
                    if (!_gotFirstMessage)
                    {
                        Console.WriteLine("RLBot is now receiving messages from Rocket League!");
                        _gotFirstMessage = true;
                        _rlbotServer.TryWrite(ServerMessage.StartCommunication());
                    }

                    _gameState = MessageHandler.CreateUpdatedState(messageClump, _gameState);

                    var matchStarted = MessageHandler.ReceivedMatchInfo(messageClump);
                    if (matchStarted)
                    {
                        _rlbotServer.TryWrite(ServerMessage.MapSpawned());
                    }

                    _rlbotServer.TryWrite(ServerMessage.DistributeGameState(_gameState));
                }
                _messengerMutex.ReleaseMutex();

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
