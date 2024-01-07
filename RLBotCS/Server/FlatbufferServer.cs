using RLBotCS.GameControl;
using RLBotCS.GameState;
using RLBotSecret.Controller;
using RLBotSecret.TCP;
using System.Net;
using System.Net.Sockets;

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
        List<FlatbufferSession> sessions = new();
        bool communicationStarted = false;

        public FlatbufferServer(int port, TcpMessenger tcpGameInterface, PlayerMapping playerMapping, MatchStarter matchStarter)
        {
            this.tcpGameInterface = tcpGameInterface;
            this.playerMapping = playerMapping;
            this.matchStarter = matchStarter;

            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();
        }

        public void StartCommunications() {
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
            catch (SocketException e)
            {
                Console.WriteLine("SocketException in Core: {0}", e);
                server.Stop();
            }
        }

        internal void SendGameStateToClients(GameState.GameState gameState)
        {
            TypedPayload gameTickPacket = gameState.gameTickPacket.ToFlatbuffer();
            List<FlatbufferSession> sessions_to_remove = new();

            foreach (FlatbufferSession session in sessions)
            {
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

                session.SetBallActorId(gameState.gameTickPacket.ball.actorId);
                session.ToggleStateSetting(matchStarter.IsStateSettingEnabled());

                try
                {
                    session.SendPayloadToClient(gameTickPacket);
                }
                catch (IOException e)
                {
                    Console.WriteLine("Core is dropping connection to session due to: {0}", e);
                    sessions_to_remove.Add(session);
                    // tell the socket to close
                    session.Close();
                }
            }

            // remove sessions that have disconnected
            foreach (FlatbufferSession session in sessions_to_remove)
            {
                sessions.Remove(session);
            }
        }

        public void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();

            var playerInputSender = new PlayerInputSender(tcpGameInterface);
            var renderingSender = new RenderingSender(tcpGameInterface);
            var gameController = new GameController(playerInputSender, renderingSender, matchStarter);

            var session = new FlatbufferSession(stream, gameController, playerMapping);
            sessions.Add(session);

            // wait until we get our first message from Rocket Leauge
            // before we start accepting messages from the client
            // they might make us do things before the game is ready
            while (!communicationStarted) {
                Thread.Sleep(100);
            }
            session.RunBlocking();
        }
    }
}
