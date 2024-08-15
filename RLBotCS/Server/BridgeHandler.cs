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

                float prevTime = _context.GameState.SecondsElapsed;
                _context.GameState = MessageHandler.CreateUpdatedState(
                    messageClump,
                    _context.GameState
                );

                float deltaTime = _context.GameState.SecondsElapsed - prevTime;
                bool timeAdvanced = deltaTime > 0.001;

                if (timeAdvanced)
                    _context.PerfMonitor.AddRLBotSample(deltaTime);
                _context.Writer.TryWrite(
                    new DistributeGameState(_context.GameState, timeAdvanced)
                );

                var matchStarted = MessageHandler.ReceivedMatchInfo(messageClump);
                if (matchStarted)
                {
                    // _context.Logger.LogInformation("Map name: " + _context.GameState.MapName);
                    _context.RenderingMgmt.ClearAllRenders(_context.MatchCommandSender);
                    _context.MatchHasStarted = true;
                    _context.Writer.TryWrite(new MapSpawned());
                }

                if (_context.GameState.MatchEnded)
                {
                    // reset everything
                    _context.QuickChat.ClearChats();
                    _context.PerfMonitor.ClearAll();
                    _context.RenderingMgmt.ClearAllRenders(_context.MatchCommandSender);
                }
                else if (
                    _context.GameState.GameStateType != GameStateType.Replay
                    && _context.GameState.GameStateType != GameStateType.Paused
                    && timeAdvanced
                )
                {
                    // only rerender if we're not in a replay or paused
                    _context.QuickChat.RenderChats(_context.RenderingMgmt, _context.GameState);

                    // only render if we're not in a goal scored or ended state
                    if (_context.GameState.GameStateType != GameStateType.GoalScored)
                        _context.PerfMonitor.RenderSummary(
                            _context.RenderingMgmt,
                            _context.GameState,
                            deltaTime
                        );
                    else
                        _context.PerfMonitor.ClearAll();
                }

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

                    _context.Logger.LogInformation("Sending delayed match commands");
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
                _context.RenderingMgmt.ClearAllRenders(_context.MatchCommandSender);
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
