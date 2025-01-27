using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;
using RLBotCS.Server.FlatbuffersMessage;

namespace RLBotCS.Server;

class FlatBuffersServer(
    int rlbotPort,
    Channel<IServerMessage, IServerMessage> incomingMessages,
    MatchStarter matchStarter,
    ChannelWriter<IBridgeMessage> bridge
)
{
    private readonly ServerContext _context = new(incomingMessages, matchStarter, bridge)
    {
        Server = new TcpListener(new(new byte[] { 0, 0, 0, 0 }), rlbotPort),
    };

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
                sessionChannel,
                _context.IncomingMessagesWriter,
                _context.Bridge,
                _context.RenderingIsEnabled,
                _context.StateSettingIsEnabled
            );

            try
            {
                session.BlockingRun();
            }
            catch (IOException)
            {
                _context.Logger.LogWarning("Session suddenly terminated the connection?");
            }
            catch (Exception e)
            {
                _context.Logger.LogError($"Error in session: {e}");
            }
            finally
            {
                session.Cleanup();
            }
        });
        sessionThread.Start();

        _context.Sessions.Add(clientId, (sessionChannel.Writer, sessionThread, 0));
    }

    private async Task HandleIncomingMessages()
    {
        await foreach (IServerMessage message in _context.IncomingMessages.ReadAllAsync())
            lock (_context)
            {
                try
                {
                    var result = message.Execute(_context);
                    if (result == ServerAction.Stop)
                        return;
                }
                catch (Exception e)
                {
                    _context.Logger.LogError($"Error while handling incoming message: {e}");
                }
            }
    }

    private async Task HandleServer()
    {
        if (_context.Server == null)
            throw new InvalidOperationException("Server not initialized");
        _context.Server.Start();

        BallPredictor.SetMode(PredictionMode.Standard);

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
        foreach (var (writer, _, _) in _context.Sessions.Values)
            writer.TryComplete();

        // Ensure all sessions are stopped
        foreach (var (_, thread, _) in _context.Sessions.Values)
            thread.Join();

        _context.Sessions.Clear();

        if (_context.Server == null)
            throw new InvalidOperationException("Server not initialized");
        _context.Server.Stop();
    }
}
