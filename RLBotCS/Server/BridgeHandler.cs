using Bridge.Conversion;
using Bridge.TCP;
using RLBotCS.Server.FlatbuffersMessage;
using System.Threading.Channels;
using GameStateType = Bridge.Models.Message.GameStateType;

namespace RLBotCS.Server;

internal class BridgeHandler(
    ChannelWriter<IServerMessage> writer,
    ChannelReader<IBridgeMessage> reader,
    TcpMessenger messenger
)
{
    private readonly BridgeContext _context = new(writer, reader, messenger);

    private async Task HandleIncomingMessages()
    {
        await foreach (IBridgeMessage message in _context.Reader.ReadAllAsync())
            lock (_context)
                message.HandleMessage(_context);
    }

    private async Task HandleServer()
    {
        await _context.Messenger.WaitForConnectionAsync();

        await foreach (var messageClump in _context.Messenger.ReadAllAsync())
        {
            lock (_context)
            {
                if (!_context.GotFirstMessage)
                {
                    _context.GotFirstMessage = true;
                    _context.Writer.TryWrite(new StartCommunication());
                }

                _context.GameState = MessageHandler.CreateUpdatedState(messageClump, _context.GameState);

                var matchStarted = MessageHandler.ReceivedMatchInfo(messageClump);
                if (matchStarted)
                {
                    _context.RenderingMgmt.ClearAllRenders();
                    _context.MatchHasStarted = true;
                    _context.Writer.TryWrite(new MapSpawned());
                }

                if (_context is
                    {
                        DelayMatchCommandSend: true, QueuedMatchCommands: true, MatchHasStarted: true,
                        GameState.GameStateType: GameStateType.Paused
                    })
                {
                    // If we send the commands before the map has spawned, nothing will happen
                    _context.DelayMatchCommandSend = false;
                    _context.QueuedMatchCommands = false;

                    _context.MatchCommandSender.Send();
                }

                _context.Writer.TryWrite(new DistributeGameState(_context.GameState));
            }
        }
    }

    public void BlockingRun() => Task.WhenAny(
        Task.Run(HandleIncomingMessages),
        Task.Run(HandleServer)
    ).Wait();

    public void Cleanup()
    {
        lock (_context)
        {
            _context.Writer.TryComplete();

            try
            {
                _context.RenderingMgmt.ClearAllRenders();
            }
            finally
            {
                _context.Messenger.Dispose();
            }
        }
    }
}