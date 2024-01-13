using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using RLBotCS.GameControl;
using RLBotCS.GameState;
using RLBotSecret.Controller;
using RLBotSecret.TCP;

namespace RLBotCS.Server
{
    /**
     * Taken from https://codinginfinite.com/multi-threaded-tcp-server-core-example-csharp/
     */
    internal class FlatbufferServer
    {
        TcpListener server;
        TcpMessenger tcpGameInterface;
        PlayerMapping playerMapping;
        MatchStarter matchStarter;
        int sessionCount = 0;
        Dictionary<int, FlatbufferSession> sessions = new();
        bool communicationStarted = false;

        public FlatbufferServer(
            int port,
            TcpMessenger tcpGameInterface,
            PlayerMapping playerMapping,
            MatchStarter matchStarter
        )
        {
            this.tcpGameInterface = tcpGameInterface;
            this.playerMapping = playerMapping;
            this.matchStarter = matchStarter;

            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();
        }

        public void StartCommunications()
        {
            communicationStarted = true;
        }

        public void StartListener()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Core Flatbuffer Server waiting for client connections...");
                    TcpClient client = server.AcceptTcpClient();
                    var ipEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    Console.WriteLine("Core is now serving a client that connected from port " + ipEndpoint.Port);

                    Thread t = new Thread(() => HandleClient(client));
                    t.Start();
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Core's TCP server was terminated.");
                server.Stop();
            }
        }

        internal void SendGameStateToClients(GameState.GameState gameState)
        {
            TypedPayload gameTickPacket = gameState.gameTickPacket.ToFlatbuffer();

            // We need to make a copy of the keys because we might remove a session
            var keys_copy = sessions.Keys.ToImmutableArray();
            foreach (var i in keys_copy)
            {
                if (!sessions.ContainsKey(i))
                {
                    continue;
                }

                var session = sessions[i];

                session.SetBallActorId(gameState.gameTickPacket.ball.actorId);
                session.ToggleStateSetting(matchStarter.IsStateSettingEnabled());
                session.ToggleRendering(matchStarter.IsRenderingEnabled());

                if (!session.IsReady)
                {
                    continue;
                }

                if (session.NeedsIntroData)
                {
                    if (matchStarter.GetMatchSettings() is TypedPayload matchSettings)
                    {
                        session.SendIntroData(matchSettings, gameState);
                    }
                }

                if (gameState.MatchEnded())
                {
                    session.RemoveRenders();
                }

                try
                {
                    session.SendPayloadToClient(gameTickPacket);
                }
                catch (IOException e)
                {
                    Console.WriteLine("Core is dropping connection to session due to: {0}", e);
                    sessions.Remove(i);
                    session.Close(false);
                }
                catch (ObjectDisposedException e)
                {
                    Console.WriteLine("Core is dropping connection to session due to: {0}", e);
                    sessions.Remove(i);
                }
            }
        }

        public void Stop()
        {
            foreach (var session in sessions.Values)
            {
                session.Close(false);
            }

            sessions.Clear();
            server.Stop();
            Thread.Sleep(100);
            tcpGameInterface.Dispose();
        }

        public void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();

            var playerInputSender = new PlayerInputSender(tcpGameInterface);
            var renderingSender = new RenderingSender(tcpGameInterface);
            var gameController = new GameController(playerInputSender, renderingSender, matchStarter);

            var session = new FlatbufferSession(stream, gameController, playerMapping);
            var id = Interlocked.Increment(ref sessionCount);
            sessions.Add(id, session);

            // wait until we get our first message from Rocket Leauge
            // before we start accepting messages from the client
            // they might make us do things before the game is ready
            while (!communicationStarted)
            {
                Thread.Sleep(100);
            }

            var wasDroppedCleanly = true;

            try
            {
                session.RunBlocking();
            }
            catch (EndOfStreamException)
            {
                Console.WriteLine("Client unexpectedly terminated it's connection to core, dropping session.");
                wasDroppedCleanly = false;
            }
            catch (IOException e) when (e.InnerException is SocketException)
            {
                Console.WriteLine("Client unexpectedly terminated it's connection to core, dropping session.");
                wasDroppedCleanly = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Core is dropping connection to session due to: {0}", e);
                wasDroppedCleanly = false;
            }

            sessions.Remove(id);
            session.Close(wasDroppedCleanly);
            client.Close();
        }
    }
}
