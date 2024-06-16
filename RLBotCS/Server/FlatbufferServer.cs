using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using rlbot.flat;
using RLBotCS.GameControl;
using RLBotSecret.Models.Message;
using RLBotSecret.State;
using GoalInfo = RLBotSecret.Packet.GoalInfo;

namespace RLBotCS.Server
{
    internal record ServerMessage
    {
        public record StartCommunication : ServerMessage;
        public record IntroDataRequest(ChannelWriter<MatchSettingsT> MatchSettingsWriter, ChannelWriter<FieldInfoT> FieldInfoWriter) : ServerMessage;
        public record DistributeGameState(GameState GameState) : ServerMessage;
        public record StartMatch(MatchSettingsT MatchSettings) : ServerMessage;
        public record MapSpawned : ServerMessage;
        public record SessionClosed(int ClientId) : ServerMessage;
        public record StopMatch(bool ShutdownServer) : ServerMessage;
    }

    internal class FlatbufferServer
    {
        private TcpListener _server;
        private ChannelReader<ServerMessage> _incomingMessages;
        private ChannelWriter<ServerMessage> _incomingMessagesWriter;
        private Dictionary<int, (ChannelWriter<SessionMessage> writer, Thread thread)> _sessions = [];

        private FieldInfoT? _fieldInfo = null;
        private bool _shouldUpdateFieldInfo = false;
        private List<ChannelWriter<MatchSettingsT>> _matchSettingsWriters = [];
        private List<ChannelWriter<FieldInfoT>> _fieldInfoWriters = [];

        private MatchStarter _matchStarter;
        private ChannelWriter<IBridgeMessage> _bridge;

        public FlatbufferServer(
            int rlbotPort,
            Channel<ServerMessage> incomingMessages,
            MatchStarter matchStarter,
            ChannelWriter<IBridgeMessage> bridge
        )
        {
            _incomingMessages = incomingMessages.Reader;
            _incomingMessagesWriter = incomingMessages.Writer;
            _matchStarter = matchStarter;
            _bridge = bridge;

            IPAddress rlbotClients = new(new byte[] { 0, 0, 0, 0 });
            _server = new TcpListener(rlbotClients, rlbotPort);
        }

        private void StopSessions()
        {
            // send stop message to all sessions
            foreach (var (writer, _) in _sessions.Values)
                writer.TryComplete();

            // ensure all sessions are stopped
            foreach (var (_, thread) in _sessions.Values)
                thread.Join();

            // remove all sessions
            _sessions.Clear();
        }

        private void UpdateFieldInfo(GameState gameState)
        {
            if (!_shouldUpdateFieldInfo)
                return;

            if (_fieldInfo == null)
                _fieldInfo = new FieldInfoT { Goals = [], BoostPads = [] };
            else
            {
                _fieldInfo.Goals.Clear();
                _fieldInfo.BoostPads.Clear();
            }

            foreach (GoalInfo goal in gameState.Goals)
            {
                _fieldInfo.Goals.Add(
                    new GoalInfoT
                    {
                        TeamNum = goal.Team,
                        Location = new Vector3T { X = goal.Location.x, Y = goal.Location.y, Z = goal.Location.z },
                        Direction = new Vector3T
                        {
                            X = goal.Direction.x, Y = goal.Direction.y, Z = goal.Direction.z
                        },
                        Width = goal.Width,
                        Height = goal.Height,
                    }
                );
            }

            foreach (BoostPadSpawn boostPad in gameState.BoostPads)
            {
                _fieldInfo.BoostPads.Add(
                    new BoostPadT
                    {
                        Location = new Vector3T
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

            foreach (var (writer, _) in _sessions.Values)
            {
                SessionMessage message = SessionMessage.DistributeGameState(gameState);
                writer.TryWrite(message);
            }
        }

        private void AddSession(TcpClient client)
        {
            Channel<SessionMessage> sessionChannel = Channel.CreateUnbounded<SessionMessage>();
            client.NoDelay = true;

            int clientId = client.Client.Handle.ToInt32();

            Thread sessionThread = new(() =>
            {
                FlatbufferSession session = new(
                    client,
                    clientId,
                    sessionChannel.Reader,
                    _incomingMessagesWriter,
                    _bridge
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
                switch (message)
                {
                    case ServerMessage.StartCommunication:
                        StartCommunication();
                        break;
                    case ServerMessage.IntroDataRequest m:
                        IntroDataRequest(m);
                        break;
                    case ServerMessage.StartMatch m:
                        StartMatch(m);
                        break;
                    case ServerMessage.DistributeGameState m:
                        DistGameState(m);
                        break;
                    case ServerMessage.MapSpawned:
                        MapSpawned();
                        break;
                    case ServerMessage.SessionClosed m:
                        SessionClosed(m);
                        break;
                    case ServerMessage.StopMatch m:
                        if (StopMatch(m))
                            return;
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

        private void StartCommunication() => _matchStarter.StartCommunication();

        private void IntroDataRequest(ServerMessage.IntroDataRequest message)
        {
            ChannelWriter<MatchSettingsT> matchSettingsWriter = message.MatchSettingsWriter;
            ChannelWriter<FieldInfoT> fieldInfoWriter = message.FieldInfoWriter;

            if (_matchStarter.GetMatchSettings() is { } settings)
            {
                matchSettingsWriter.TryWrite(settings);
                matchSettingsWriter.TryComplete();
            }
            else
                _matchSettingsWriters.Add(matchSettingsWriter);

            if (_fieldInfo != null)
            {
                fieldInfoWriter.TryWrite(_fieldInfo);
                fieldInfoWriter.TryComplete();
            }
            else
                _fieldInfoWriters.Add(fieldInfoWriter);
        }

        private void StartMatch(ServerMessage.StartMatch message)
        {
            MatchSettingsT matchSettings = message.MatchSettings;
            _matchStarter.StartMatch(matchSettings);

            // distribute the match settings to all waiting sessions
            foreach (var writer in _matchSettingsWriters)
            {
                writer.TryWrite(matchSettings);
                writer.TryComplete();
            }

            _matchSettingsWriters.Clear();
        }

        private void DistGameState(ServerMessage.DistributeGameState message)
        {
            GameState gameState = message.GameState;
            UpdateFieldInfo(gameState);
            DistributeGameState(gameState);
        }

        private void MapSpawned()
        {
            _fieldInfo = null;
            _shouldUpdateFieldInfo = true;
            _matchStarter.MapSpawned();
        }

        private void SessionClosed(ServerMessage.SessionClosed message)
        {
            _sessions.Remove(message.ClientId);
            Console.WriteLine("Session closed.");
        }

        private bool StopMatch(ServerMessage.StopMatch message)
        {
            _matchStarter.NullMatchSettings();
            _fieldInfo = null;
            _shouldUpdateFieldInfo = false;

            if (message.ShutdownServer)
            {
                _incomingMessagesWriter.TryComplete();
                return true;
            }

            StopSessions();
            return false;
        }
    }
}