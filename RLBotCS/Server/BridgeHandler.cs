using System.Threading.Channels;
using Bridge.Conversion;
using Bridge.TCP;
using Microsoft.Extensions.Logging;
using RLBotCS.Server.FlatbuffersMessage;
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
            {
                try
                {
                    message.HandleMessage(_context);
                }
                catch (Exception e)
                {
                    _context.Logger.LogError(
                        $"Error while handling message in BridgeHandler: {e}"
                    );
                }
            }
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

                // reset the counter that lets us know if we're sending too many bytes
                messenger.ResetByteCount();

                _context.GameState = MessageHandler.CreateUpdatedState(
                    messageClump,
                    _context.GameState
                );

                _context.Writer.TryWrite(new DistributeGameState(_context.GameState));

                var matchStarted = MessageHandler.ReceivedMatchInfo(messageClump);
                if (matchStarted)
                {
                    _context.RenderingMgmt.ClearAllRenders();
                    _context.MatchHasStarted = true;
                    _context.Writer.TryWrite(new MapSpawned());
                }

                if (_context.GameState.MatchEnded)
                {
                    if (!_context.LastMatchEnded)
                    {
                        _context.QuickChat.ClearChats();
                        _context.RenderingMgmt.ClearAllRenders();
                        _context.Writer.TryWrite(new StopMatch(false));
                    }
                }
                else
                {
                    _context.QuickChat.RenderChats(_context.RenderingMgmt, _context.GameState);
                }
                _context.LastMatchEnded = _context.GameState.MatchEnded;

                if (
                    _context is
                    {
                        DelayMatchCommandSend: true,
                        QueuedMatchCommands: true,
                        MatchHasStarted: true,
                        GameState.GameStateType: GameStateType.Paused
                    }
                )
                {
                    // If we send the commands before the map has spawned, nothing will happen
                    _context.DelayMatchCommandSend = false;
                    _context.QueuedMatchCommands = false;

                    _context.MatchCommandSender.Send();
                }

                _context.RenderingMgmt.SendRenderClears();
            }
        }
    }

    public void BlockingRun() =>
        Task.WhenAny(Task.Run(HandleIncomingMessages), Task.Run(HandleServer)).Wait();

    public void Cleanup()
    {
        lock (_context)
        {
            _context.Writer.TryComplete();

            try
            {
                _context.RenderingMgmt.ClearAllRenders();

                // we can only clear so many renders each tick
                // so we do this until we've cleared them all
                // or rocket league has been closed
                while (!_context.RenderingMgmt.SendRenderClears())
                {
                    if (!messenger.WaitForAnyMessageAsync().Result)
                        break;

                    messenger.ResetByteCount();
                }
            }
            catch (Exception e)
            {
                _context.Logger.LogError($"Error while cleaning up BridgeHandler: {e}");
            }
            finally
            {
                _context.Messenger.Dispose();
            }
        }
    }
}
