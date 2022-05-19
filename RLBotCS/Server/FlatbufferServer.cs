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
        List<FlatbufferSession> sessions = new();

        public FlatbufferServer(int port, TcpMessenger tcpGameInterface, PlayerMapping playerMapping)
        {
            this.tcpGameInterface = tcpGameInterface;
            this.playerMapping = playerMapping;

            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();
        }

        public void StartListener()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("RLBotCS Flatbuffer Server waiting for client connections...");
                    TcpClient client = server.AcceptTcpClient();
                    var ipEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    Console.WriteLine("RLBotCS is now serving a client that connected from port " + ipEndpoint.Port);

                    Thread t = new Thread(() => HandleClient(client));
                    t.Start();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }

        internal void SendGameStateToClients(GameState.GameState gameState)
        {
            TypedPayload gameTickPacket = gameState.gameTickPacket.ToFlatbuffer();
            foreach (FlatbufferSession session in sessions)
            {
                if (!session.IsReady)
                {
                    continue;
                }
                if (session.NeedsIntroData)
                {
                    // TODO: send intro data like match settings, field info packet
                }

                
                session.SendPayloadToClient(gameTickPacket);
            }
        }

        public void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();

            var playerInputSender = new PlayerInputSender(tcpGameInterface);
            var renderingSender = new RenderingSender(tcpGameInterface);
            var gameController = new GameController(playerInputSender, renderingSender);

            var session = new FlatbufferSession(stream, gameController, playerMapping);
            sessions.Add(session);
            session.RunBlocking();
        }
    }
}
