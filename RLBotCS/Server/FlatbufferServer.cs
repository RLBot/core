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

    internal class ServerContext(
        Channel<ServerMessage> incomingMessages,
        MatchStarter matchStarter,
        ChannelWriter<IBridgeMessage> bridge)
    {
        public TcpListener Server { get; set; }
        public ChannelReader<ServerMessage> IncomingMessages { get; } = incomingMessages.Reader;
        public ChannelWriter<ServerMessage> IncomingMessagesWriter { get; } = incomingMessages.Writer;
        public Dictionary<int, (ChannelWriter<SessionMessage> writer, Thread thread)> Sessions { get; } = [];

        public FieldInfoT? FieldInfo { get; set; }
        public bool ShouldUpdateFieldInfo { get; set; }
        public List<ChannelWriter<MatchSettingsT>> MatchSettingsWriters { get; } = [];
        public List<ChannelWriter<FieldInfoT>> FieldInfoWriters { get; } = [];

        public MatchStarter MatchStarter { get; } = matchStarter;
        public ChannelWriter<IBridgeMessage> Bridge { get; } = bridge;

        public void StopSessions()
        {
            // Send stop message to all sessions
            foreach (var (writer, _) in Sessions.Values)
                writer.TryComplete();

            // Ensure all sessions are stopped
            foreach (var (_, thread) in Sessions.Values)
                thread.Join();

            Sessions.Clear();
        }
    }

    internal class FlatbufferServer
    {
        private readonly ServerContext _context;

        public FlatbufferServer(
            int rlbotPort,
            Channel<ServerMessage> incomingMessages,
            MatchStarter matchStarter,
            ChannelWriter<IBridgeMessage> bridge
        )
        {
            IPAddress rlbotClients = new(new byte[] { 0, 0, 0, 0 });
            _context = new ServerContext(incomingMessages, matchStarter, bridge)
            {
                Server = new TcpListener(rlbotClients, rlbotPort)
            };
        }

        private void UpdateFieldInfo(GameState gameState)
        {
            if (!_context.ShouldUpdateFieldInfo)
                return;

            if (_context.FieldInfo == null)
                _context.FieldInfo = new FieldInfoT { Goals = [], BoostPads = [] };
            else
            {
                _context.FieldInfo.Goals.Clear();
                _context.FieldInfo.BoostPads.Clear();
            }

            foreach (GoalInfo goal in gameState.Goals)
            {
                _context.FieldInfo.Goals.Add(
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
                _context.FieldInfo.BoostPads.Add(
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
            foreach (var writer in _context.FieldInfoWriters)
            {
                writer.TryWrite(_context.FieldInfo);
                writer.TryComplete();
            }

            _context.FieldInfoWriters.Clear();
        }

        private void DistributeGameState(GameState gameState)
        {
            _context.MatchStarter.matchEnded = gameState.MatchEnded;

            foreach (var (writer, _) in _context.Sessions.Values)
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
                    _context.IncomingMessagesWriter,
                    _context.Bridge
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

            _context.Sessions.Add(clientId, (sessionChannel.Writer, sessionThread));
        }

        private async Task HandleIncomingMessages()
        {
            await foreach (ServerMessage message in _context.IncomingMessages.ReadAllAsync())
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
            _context.Server.Start();

            while (true)
            {
                TcpClient client = await _context.Server.AcceptTcpClientAsync();
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
            _context.StopSessions();
            _context.Server.Stop();
        }

        private void StartCommunication() => _context.MatchStarter.StartCommunication();

        private void IntroDataRequest(ServerMessage.IntroDataRequest message)
        {
            ChannelWriter<MatchSettingsT> matchSettingsWriter = message.MatchSettingsWriter;
            ChannelWriter<FieldInfoT> fieldInfoWriter = message.FieldInfoWriter;

            if (_context.MatchStarter.GetMatchSettings() is { } settings)
            {
                matchSettingsWriter.TryWrite(settings);
                matchSettingsWriter.TryComplete();
            }
            else
                _context.MatchSettingsWriters.Add(matchSettingsWriter);

            if (_context.FieldInfo != null)
            {
                fieldInfoWriter.TryWrite(_context.FieldInfo);
                fieldInfoWriter.TryComplete();
            }
            else
                _context.FieldInfoWriters.Add(fieldInfoWriter);
        }

        private void StartMatch(ServerMessage.StartMatch message)
        {
            MatchSettingsT matchSettings = message.MatchSettings;
            _context.MatchStarter.StartMatch(matchSettings);

            // distribute the match settings to all waiting sessions
            foreach (var writer in _context.MatchSettingsWriters)
            {
                writer.TryWrite(matchSettings);
                writer.TryComplete();
            }

            _context.MatchSettingsWriters.Clear();
        }

        private void DistGameState(ServerMessage.DistributeGameState message)
        {
            GameState gameState = message.GameState;
            UpdateFieldInfo(gameState);
            DistributeGameState(gameState);
        }

        private void MapSpawned()
        {
            _context.FieldInfo = null;
            _context.ShouldUpdateFieldInfo = true;
            _context.MatchStarter.MapSpawned();
        }

        private void SessionClosed(ServerMessage.SessionClosed message)
        {
            _context.Sessions.Remove(message.ClientId);
            Console.WriteLine("Session closed.");
        }

        private bool StopMatch(ServerMessage.StopMatch message)
        {
            _context.MatchStarter.NullMatchSettings();
            _context.FieldInfo = null;
            _context.ShouldUpdateFieldInfo = false;

            if (message.ShutdownServer)
            {
                _context.IncomingMessagesWriter.TryComplete();
                return true;
            }

            _context.StopSessions();
            return false;
        }
    }
}