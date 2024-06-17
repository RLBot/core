using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using RLBotCS.GameControl;
using RLBotCS.Server.FlatbuffersMessage;


namespace RLBotCS.Server;

internal class FlatBuffersServer
{
    private readonly ServerContext _context;

    public FlatBuffersServer(
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
            FlatBuffersSession session = new(
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
            lock (_context)
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
        // Send stop message to all sessions
        foreach (var (writer, _) in _context.Sessions.Values)
            writer.TryComplete();

        // Ensure all sessions are stopped
        foreach (var (_, thread) in _context.Sessions.Values)
            thread.Join();

        _context.Sessions.Clear();

        if (_context.Server == null)
            throw new InvalidOperationException("Server not initialized");
        _context.Server.Stop();
    }
}