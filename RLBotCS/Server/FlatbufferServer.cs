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
    internal enum ServerAction
    {
        Continue,
        Stop
    }

    internal interface IServerMessage
    {
        public ServerAction Execute(ServerContext context);
    }

    internal record StartCommunication : IServerMessage
    {
        public ServerAction Execute(ServerContext context)
        {
            context.MatchStarter.StartCommunication();
            return ServerAction.Continue;
        }
    }

    internal record IntroDataRequest(
        ChannelWriter<MatchSettingsT> MatchSettingsWriter,
        ChannelWriter<FieldInfoT> FieldInfoWriter) : IServerMessage
    {
        public ServerAction Execute(ServerContext context)
        {
            if (context.MatchStarter.GetMatchSettings() is { } settings)
            {
                MatchSettingsWriter.TryWrite(settings);
                MatchSettingsWriter.TryComplete();
            }
            else
                context.MatchSettingsWriters.Add(MatchSettingsWriter);

            if (context.FieldInfo != null)
            {
                FieldInfoWriter.TryWrite(context.FieldInfo);
                FieldInfoWriter.TryComplete();
            }
            else
                context.FieldInfoWriters.Add(FieldInfoWriter);

            return ServerAction.Continue;
        }
    }

    internal record DistributeGameState(GameState GameState) : IServerMessage
    {
        private static void UpdateFieldInfo(ServerContext context, GameState gameState)
        {
            if (!context.ShouldUpdateFieldInfo)
                return;

            if (context.FieldInfo == null)
                context.FieldInfo = new FieldInfoT { Goals = [], BoostPads = [] };
            else
            {
                context.FieldInfo.Goals.Clear();
                context.FieldInfo.BoostPads.Clear();
            }

            foreach (GoalInfo goal in gameState.Goals)
            {
                context.FieldInfo.Goals.Add(
                    new GoalInfoT
                    {
                        TeamNum = goal.Team,
                        Location =
                            new Vector3T { X = goal.Location.x, Y = goal.Location.y, Z = goal.Location.z },
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
                context.FieldInfo.BoostPads.Add(
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
            foreach (var writer in context.FieldInfoWriters)
            {
                writer.TryWrite(context.FieldInfo);
                writer.TryComplete();
            }

            context.FieldInfoWriters.Clear();
        }

        private static void DistributeState(ServerContext context, GameState gameState)
        {
            context.MatchStarter.matchEnded = gameState.MatchEnded;

            foreach (var (writer, _) in context.Sessions.Values)
            {
                SessionMessage message = SessionMessage.DistributeGameState(gameState);
                writer.TryWrite(message);
            }
        }

        public ServerAction Execute(ServerContext context)
        {
            UpdateFieldInfo(context, GameState);
            DistributeState(context, GameState);

            return ServerAction.Continue;
        }
    }

    internal record StartMatch(MatchSettingsT MatchSettings) : IServerMessage
    {
        public ServerAction Execute(ServerContext context)
        {
            context.MatchStarter.StartMatch(MatchSettings);

            // Distribute the match settings to all waiting sessions
            foreach (var writer in context.MatchSettingsWriters)
            {
                writer.TryWrite(MatchSettings);
                writer.TryComplete();
            }

            context.MatchSettingsWriters.Clear();

            return ServerAction.Continue;
        }
    }

    internal record MapSpawned : IServerMessage
    {
        public ServerAction Execute(ServerContext context)
        {
            context.FieldInfo = null;
            context.ShouldUpdateFieldInfo = true;
            context.MatchStarter.MapSpawned();

            return ServerAction.Continue;
        }
    }

    internal record SessionClosed(int ClientId) : IServerMessage
    {
        public ServerAction Execute(ServerContext context)
        {
            context.Sessions.Remove(ClientId);
            Console.WriteLine("Session closed.");

            return ServerAction.Continue;
        }
    }

    internal record StopMatch(bool ShutdownServer) : IServerMessage
    {
        public ServerAction Execute(ServerContext context)
        {
            context.MatchStarter.NullMatchSettings();
            context.FieldInfo = null;
            context.ShouldUpdateFieldInfo = false;

            if (ShutdownServer)
            {
                context.IncomingMessagesWriter.TryComplete();
                return ServerAction.Stop;
            }

            context.StopSessions();
            return ServerAction.Continue;
        }
    }


    internal class ServerContext(
        Channel<IServerMessage> incomingMessages,
        MatchStarter matchStarter,
        ChannelWriter<IBridgeMessage> bridge)
    {
        public TcpListener? Server { get; set; }
        public ChannelReader<IServerMessage> IncomingMessages { get; } = incomingMessages.Reader;
        public ChannelWriter<IServerMessage> IncomingMessagesWriter { get; } = incomingMessages.Writer;
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
            Channel<IServerMessage> incomingMessages,
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
            await foreach (IServerMessage message in _context.IncomingMessages.ReadAllAsync())
            {
                var result = message.Execute(_context);
                if (result == ServerAction.Stop)
                    return;
            }
        }

        private async Task HandleServer()
        {
            if (_context.Server == null)
                throw new InvalidOperationException("Server not initialized");
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

            if (_context.Server == null)
                throw new InvalidOperationException("Server not initialized");
            _context.Server.Stop();
        }
    }
}