using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;
using RLBotCS.Server.ServerMessage;

namespace RLBotCS.Server;

class FlatBuffersServer(
    int rlbotPort,
    Channel<IServerMessage, IServerMessage> incomingMessages,
    ChannelWriter<IBridgeMessage> bridge
)
{
    private readonly ServerContext _context = new(incomingMessages, bridge)
    {
        Server = new TcpListener(IPAddress.IPv6Any, rlbotPort),
    };

    private void AddSession(TcpClient client)
    {
        Channel<SessionMessage> sessionChannel = Channel.CreateBounded<SessionMessage>(60);
        client.NoDelay = true;

        int clientId = client.Client.Handle.ToInt32();
        while (_context.Sessions.ContainsKey(clientId))
        {
            clientId++;
        }

        Thread sessionThread = new(() =>
        {
            _context.Logger.LogDebug("Client {} connected", clientId);

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

        _context.Sessions.Add(clientId, (sessionChannel.Writer, sessionThread));
        sessionThread.Start();
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
        try
        {
            if (_context.Server == null)
                throw new InvalidOperationException("Server not initialized");
            _context.Server.Server.DualMode = true;
            _context.Server.Start();

            BallPredictor.SetMode(PredictionMode.Standard);

            while (true)
            {
                TcpClient client = await _context.Server.AcceptTcpClientAsync();

                lock (_context)
                {
                    AddSession(client);
                }
            }
        }
        catch (Exception e)
        {
            _context.Logger.LogError($"Error while handling server: {e}");
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
        _context.Logger.LogDebug("Shutting down FlatBuffersServer");

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
