using System.Net;
using System.Net.Sockets;

namespace RLBotCS.Server
{

    /**
     * Taken from https://codinginfinite.com/multi-threaded-tcp-server-core-example-csharp/
     */
    internal class FlatbufferServer
    {

        TcpListener server = null;
        public FlatbufferServer(int port)
        {
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();
            StartListener();
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

        public void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var session = new FlatbufferSession(stream);
            session.RunBlocking();
        }
    }
}
