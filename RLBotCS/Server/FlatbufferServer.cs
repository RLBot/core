using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using rlbot.flat;
using RLBotCS.GameControl;
using RLBotSecret.State;
using RLBotSecret.Types;

namespace RLBotCS.Server
{
    internal enum ServerMessageType
    {
        StartCommunication,
        DistributeGameState,
        StartMatch,
        MapSpawned,
        SessionClosed,
        StopMatch,
    }

    internal class ServerMessage
    {
        private ServerMessageType _type;

        private GameState _gameState;
        private TypedPayload _matchSettingsPayload;
        private MatchSettingsT _matchSettings;
        private int _clientId;
        private bool _shutdownServer;

        public static ServerMessage StartCommunication()
        {
            return new ServerMessage { _type = ServerMessageType.StartCommunication };
        }

        public static ServerMessage DistributeGameState(GameState gameState)
        {
            return new ServerMessage { _type = ServerMessageType.DistributeGameState, _gameState = gameState };
        }

        public static ServerMessage StartMatch(TypedPayload matchSettingsPayload, MatchSettingsT matchSettings)
        {
            return new ServerMessage
            {
                _type = ServerMessageType.StartMatch,
                _matchSettings = matchSettings,
                _matchSettingsPayload = matchSettingsPayload
            };
        }

        public static ServerMessage MapSpawned()
        {
            return new ServerMessage { _type = ServerMessageType.MapSpawned };
        }

        public static ServerMessage SessionClosed(int clientId)
        {
            return new ServerMessage { _type = ServerMessageType.SessionClosed, _clientId = clientId };
        }

        public static ServerMessage StopMatch(bool shutdownServer)
        {
            return new ServerMessage { _type = ServerMessageType.StopMatch, _shutdownServer = shutdownServer };
        }

        public ServerMessageType Type()
        {
            return _type;
        }

        public GameState GetGameState()
        {
            return _gameState;
        }

        public MatchSettingsT GetMatchSettings()
        {
            return _matchSettings;
        }

        public TypedPayload GetMatchSettingsPayload()
        {
            return _matchSettingsPayload;
        }

        public int GetClientId()
        {
            return _clientId;
        }

        public bool GetShutdownServer()
        {
            return _shutdownServer;
        }
    }

    internal class FlatbufferServer
    {
        private TcpListener _server;
        private ChannelReader<ServerMessage> _incomingMessages;
        private ChannelWriter<ServerMessage> _incomingMessagesWriter;
        private Dictionary<int, (ChannelWriter<SessionMessage>, Thread)> _sessions = new();

        private int _gamePort;
        private TypedPayload? _matchSettingsPayload;
        private MatchStarter _matchStarter;

        public FlatbufferServer(
            int gamePort,
            int rlbotPort,
            Channel<ServerMessage> incomingMessages,
            MatchStarter matchStarter
        )
        {
            _gamePort = gamePort;
            _incomingMessages = incomingMessages.Reader;
            _incomingMessagesWriter = incomingMessages.Writer;
            _matchStarter = matchStarter;

            IPAddress rlbotClients = new IPAddress(new byte[] { 0, 0, 0, 0 });
            _server = new TcpListener(rlbotClients, rlbotPort);
        }

        private void StopSessions()
        {
            // send stop message to all sessions
            foreach (var session in _sessions.Values)
            {
                session.Item1.TryComplete();
            }

            // ensure all sessions are stopped
            foreach (var session in _sessions.Values)
            {
                session.Item2.Join();
            }

            // remove all sessions
            _sessions.Clear();
        }

        private void DistributeGameState(GameState gameState)
        {
            _matchStarter.matchEnded = gameState.MatchEnded;

            foreach (var session in _sessions.Values)
            {
                SessionMessage message = SessionMessage.DistributeGameState(gameState.ToFlatbuffer());
                session.Item1.TryWrite(message);
            }
        }

        private void AddSession(TcpClient client)
        {
            Channel<SessionMessage> sessionChannel = Channel.CreateUnbounded<SessionMessage>();
            client.NoDelay = true;

            int clientId = client.Client.Handle.ToInt32();

            Thread sessionThread = new Thread(() =>
            {
                FlatbufferSession session = new FlatbufferSession(
                    client,
                    clientId,
                    sessionChannel.Reader,
                    _incomingMessagesWriter
                );
                session.BlockingRun();
                session.Cleanup();
            });
            sessionThread.Start();

            _sessions.Add(clientId, (sessionChannel.Writer, sessionThread));
            Console.WriteLine("New session added.");
        }

        public void BlockingRun()
        {
            SpinWait spinWait = new SpinWait();
            _server.Start();

            while (!_incomingMessages.Completion.IsCompleted)
            {
                bool handledSomething = false;

                // start listening to the channel
                while (_incomingMessages.TryRead(out ServerMessage message))
                {
                    handledSomething = true;

                    switch (message.Type())
                    {
                        case ServerMessageType.StartCommunication:
                            _matchStarter.StartCommunication();
                            break;
                        case ServerMessageType.StartMatch:
                            _matchSettingsPayload = message.GetMatchSettingsPayload();
                            _matchStarter.StartMatch(message.GetMatchSettings());
                            break;
                        case ServerMessageType.DistributeGameState:
                            DistributeGameState(message.GetGameState());
                            break;
                        case ServerMessageType.MapSpawned:
                            _matchStarter.MapSpawned();
                            break;
                        case ServerMessageType.SessionClosed:
                            _sessions.Remove(message.GetClientId());
                            Console.WriteLine("Session closed.");
                            break;
                        case ServerMessageType.StopMatch:
                            if (message.GetShutdownServer())
                            {
                                _incomingMessagesWriter.TryComplete();
                                return;
                            }

                            StopSessions();
                            break;
                    }
                }

                // try to accept a new tcp client
                if (_server.Pending())
                {
                    handledSomething = true;
                    AddSession(_server.AcceptTcpClient());
                }

                if (!handledSomething)
                {
                    // prevent busy waiting
                    spinWait.SpinOnce();
                }
            }
        }

        public void Cleanup()
        {
            Console.WriteLine("Server channel was closed.");
            StopSessions();
            _server.Stop();
        }
    }
}
