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
        IntroDataRequest,
        DistributeGameState,
        StartMatch,
        MapSpawned,
        SessionClosed,
        StopMatch,
    }

    internal class ServerMessage
    {
        private ServerMessageType _type;

        private ChannelWriter<TypedPayload> _matchSettingsWriter;
        private ChannelWriter<FieldInfoT> _fieldInfoWriter;
        private GameState _gameState;
        private TypedPayload _matchSettingsPayload;
        private MatchSettingsT _matchSettings;
        private int _clientId;
        private bool _shutdownServer;

        public static ServerMessage StartCommunication()
        {
            return new ServerMessage { _type = ServerMessageType.StartCommunication };
        }

        public static ServerMessage IntroDataRequest(
            ChannelWriter<TypedPayload> matchSettingsWriter,
            ChannelWriter<FieldInfoT> fieldInfoWriter
        )
        {
            return new ServerMessage
            {
                _type = ServerMessageType.IntroDataRequest,
                _matchSettingsWriter = matchSettingsWriter,
                _fieldInfoWriter = fieldInfoWriter
            };
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

        public ChannelWriter<TypedPayload> GetMatchSettingsWriter()
        {
            return _matchSettingsWriter;
        }

        public ChannelWriter<FieldInfoT> GetFieldInfoWriter()
        {
            return _fieldInfoWriter;
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

        private TypedPayload? _matchSettingsPayload = null;
        private FieldInfoT? _fieldInfo = null;
        private bool _shouldUpdateFieldInfo = false;
        private List<ChannelWriter<TypedPayload>> _matchSettingsWriters = new();
        private List<ChannelWriter<FieldInfoT>> _fieldInfoWriters = new();

        private MatchStarter _matchStarter;

        public FlatbufferServer(int rlbotPort, Channel<ServerMessage> incomingMessages, MatchStarter matchStarter)
        {
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

        private void UpdateFieldInfo(GameState gameState)
        {
            if (!_shouldUpdateFieldInfo)
            {
                return;
            }

            if (_fieldInfo == null)
            {
                _fieldInfo = new FieldInfoT() { Goals = gameState.Goals, BoostPads = new List<BoostPadT>() };
            }
            else
            {
                _fieldInfo.Goals = gameState.Goals;
                _fieldInfo.BoostPads.Clear();
            }

            foreach (var boostPad in gameState.BoostPads)
            {
                _fieldInfo.BoostPads.Add(
                    new BoostPadT
                    {
                        Location = new Vector3T()
                        {
                            X = boostPad.SpawnPosition.x,
                            Y = boostPad.SpawnPosition.y,
                            Z = boostPad.SpawnPosition.z
                        },
                        IsFullBoost = boostPad.IsFullBoost,
                    }
                );
            }

            // distribute the field info to all waiting sessions
            foreach (var writer in _fieldInfoWriters)
            {
                writer.TryWrite(_fieldInfo);
                writer.TryComplete();
            }
            _fieldInfoWriters.Clear();
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

                try
                {
                    session.BlockingRun();
                }
                catch (IOException)
                {
                    Console.WriteLine("Session suddenly terminated the connection?");
                }

                session.Cleanup();
            });
            sessionThread.Start();

            _sessions.Add(clientId, (sessionChannel.Writer, sessionThread));
        }

        private async Task HandleIncomingMessages()
        {
            await foreach (ServerMessage message in _incomingMessages.ReadAllAsync())
            {
                switch (message.Type())
                {
                    case ServerMessageType.StartCommunication:
                        _matchStarter.StartCommunication();
                        break;
                    case ServerMessageType.IntroDataRequest:
                        ChannelWriter<TypedPayload> matchSettingsWriter = message.GetMatchSettingsWriter();
                        ChannelWriter<FieldInfoT> fieldInfoWriter = message.GetFieldInfoWriter();

                        if (_matchSettingsPayload != null)
                        {
                            matchSettingsWriter.TryWrite(_matchSettingsPayload);
                            matchSettingsWriter.TryComplete();
                        }
                        else
                        {
                            _matchSettingsWriters.Add(matchSettingsWriter);
                        }

                        if (_fieldInfo != null)
                        {
                            fieldInfoWriter.TryWrite(_fieldInfo);
                            fieldInfoWriter.TryComplete();
                        }
                        else
                        {
                            _fieldInfoWriters.Add(fieldInfoWriter);
                        }

                        break;
                    case ServerMessageType.StartMatch:
                        _matchSettingsPayload = message.GetMatchSettingsPayload();
                        _matchStarter.StartMatch(message.GetMatchSettings());

                        // distribute the match settings to all waiting sessions
                        foreach (var writer in _matchSettingsWriters)
                        {
                            writer.TryWrite(_matchSettingsPayload);
                            writer.TryComplete();
                        }
                        _matchSettingsWriters.Clear();
                        break;
                    case ServerMessageType.DistributeGameState:
                        GameState gameState = message.GetGameState();

                        UpdateFieldInfo(gameState);
                        DistributeGameState(gameState);
                        break;
                    case ServerMessageType.MapSpawned:
                        _fieldInfo = null;
                        _shouldUpdateFieldInfo = true;

                        _matchStarter.MapSpawned();
                        break;
                    case ServerMessageType.SessionClosed:
                        _sessions.Remove(message.GetClientId());
                        Console.WriteLine("Session closed.");
                        break;
                    case ServerMessageType.StopMatch:
                        _matchSettingsPayload = null;
                        _fieldInfo = null;
                        _shouldUpdateFieldInfo = false;

                        if (message.GetShutdownServer())
                        {
                            _incomingMessagesWriter.TryComplete();
                            return;
                        }

                        StopSessions();
                        break;
                }
            }
        }

        private async Task HandleServer()
        {
            _server.Start();

            while (true)
            {
                TcpClient client = await _server.AcceptTcpClientAsync();
                AddSession(client);
            }
        }

        public void BlockingRun()
        {
            Task incomingMessagesTask = Task.Run(HandleIncomingMessages);
            Task serverTask = Task.Run(HandleServer);

            Task.WhenAny(incomingMessagesTask, serverTask).Wait();
        }

        public void Cleanup()
        {
            StopSessions();
            _server.Stop();
        }
    }
}
