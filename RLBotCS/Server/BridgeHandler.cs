using System.Threading.Channels;
using Bridge.Conversion;
using Bridge.TCP;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBotCS.Conversion;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;
using RLBotCS.Server.ServerMessage;
using MatchPhase = Bridge.Models.Message.MatchPhase;

namespace RLBotCS.Server;

class BridgeHandler(
    ChannelWriter<IServerMessage> writer,
    ChannelReader<IBridgeMessage> reader,
    TcpMessenger messenger,
    MatchStarter matchStarter
)
{
    private ManualResetEvent startReadingInternalMsgs = new ManualResetEvent(false);

    private const int MAX_TICK_SKIP = 1;
    private readonly BridgeContext _context = new(writer, reader, messenger, matchStarter);

    private async Task HandleInternalMessages()
    {
        // if Rocket League is already running,
        // we wait for it to connect to us first
        if (LaunchManager.IsRocketLeagueRunningWithArgs())
            startReadingInternalMsgs.WaitOne();

        _context.Logger.LogDebug("Started reading internal messages");

        await foreach (IBridgeMessage message in _context.Reader.ReadAllAsync())
        {
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
    }

    private async Task HandleServer()
    {
        await _context.Messenger.WaitForConnectionAsync();

        _context.Logger.LogInformation("Connected to Rocket League");

        bool isFirstTick = true;

        await foreach (var messageClump in _context.Messenger.ReadAllAsync())
        {
            lock (_context)
            {
                if (isFirstTick)
                {
                    isFirstTick = false;
                    // Trigger HandleInternalMessages to start processing messages
                    // it will still wait until we're done,
                    // since we have a lock on _context
                    startReadingInternalMsgs.Set();
                }

                // reset the counter that lets us know if we're sending too many bytes
                // technically this resets every time Rocket League renders a frame,
                // but we don't know when that is. Since every message from the game
                // means _at least_ one frame was rendered, this works good enough.
                messenger.ResetByteCount();
                // reset the counter that lets us know if we're sending too many clear messages
                _context.RenderingMgmt.ResetClearCount();

                float prevTime = _context.GameState.SecondsElapsed;
                _context.GameState = MessageHandler.CreateUpdatedState(
                    messageClump,
                    _context.GameState
                );

                float deltaTime = _context.GameState.SecondsElapsed - prevTime;
                bool timeAdvanced = deltaTime > 0.001;
                if (timeAdvanced)
                {
                    _context.ticksSkipped = 0;
                    _context.ticksSinceMapLoad += 1;
                }
                else
                    _context.ticksSkipped++;

                if (timeAdvanced)
                    _context.PerfMonitor.AddRLBotSample(deltaTime);

                ConsiderDistributingPacket(_context, timeAdvanced);

                _context.MatchStarter.SetCurrentMatchPhase(
                    _context.GameState.MatchPhase,
                    _context.GetPlayerSpawner()
                );

                var mapJustLoaded = MessageHandler.ReceivedMatchInfo(messageClump);
                if (mapJustLoaded)
                {
                    _context.ticksSinceMapLoad = 0;
                    if (_context.GameState.MatchPhase != MatchPhase.Paused)
                    {
                        // LAN matches don't set the MatchPhase to paused, which breaks Continue & Spawn
                        // thankfully, we can just manually set the match phase to paused
                        _context.GameState.MatchPhase = MatchPhase.Paused;
                    }

                    if (_context.MatchConfig is { AutoSaveReplay: true })
                    {
                        _context.MatchCommandQueue.AddConsoleCommand(
                            FlatToCommand.MakeAutoSaveReplayCommand()
                        );
                    }
                    _context.RenderingMgmt.ClearAllRenders();
                    _context.MatchStarter.OnMapSpawn(
                        _context.GameState.MapName,
                        _context.GetPlayerSpawner()
                    );
                    _context.Writer.TryWrite(new DistributeFieldInfo(_context.GameState));

                    // wait for the next tick,
                    // this ensures we don't start the match too soon
                    continue;
                }

                if (
                    _context.GameState.MatchPhase == MatchPhase.Ended
                    || _context.GameState.MatchPhase == MatchPhase.Inactive
                )
                {
                    // reset everything
                    _context.QuickChat.ClearChats();
                    _context.PerfMonitor.ClearAll();
                }
                else if (
                    _context.GameState.MatchPhase != MatchPhase.Replay
                    && _context.GameState.MatchPhase != MatchPhase.Paused
                    && timeAdvanced
                )
                {
                    // only rerender if we're not in a replay or paused
                    _context.QuickChat.RenderChats(_context.RenderingMgmt, _context.GameState);

                    // only render if we're not in a goal scored or ended state
                    if (_context.GameState.MatchPhase != MatchPhase.GoalScored)
                        _context.PerfMonitor.RenderSummary(
                            _context.RenderingMgmt,
                            _context.GameState,
                            deltaTime
                        );
                    else
                        _context.PerfMonitor.ClearAll();
                }

                if (
                    _context.MatchStarter.HasSpawnedMap
                    && _context.ticksSinceMapLoad >= 2
                    && _context.GameState.MatchPhase == MatchPhase.Paused
                    && _context.SpawnCommandQueue.Count > 0
                )
                {
                    _context.Logger.LogDebug("Sending queued spawning commands");
                    _context.SpawnCommandQueue.Flush();
                }

                _context.RenderingMgmt.SendRenderClears();
            }
        }
    }

    public void BlockingRun() =>
        Task.WhenAny(Task.Run(HandleInternalMessages), Task.Run(HandleServer)).Wait();

    public void Cleanup()
    {
        lock (_context)
        {
            _context.Logger.LogDebug("Shutting down BridgeHandler");
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
            catch (InvalidOperationException) { }
            catch (IOException) { }
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

    private static void ConsiderDistributingPacket(BridgeContext context, bool timeAdvanced)
    {
        var config = context.MatchConfig;
        if (config == null)
            return;

        // While game is paused (or similar), we distribute less often
        bool due =
            context
                is {
                    ticksSkipped: > MAX_TICK_SKIP,
                    GameState.MatchPhase: MatchPhase.Replay
                        or MatchPhase.Paused
                        or MatchPhase.Ended
                        or MatchPhase.Inactive
                };
        if (!timeAdvanced && !due)
            return;

        // We only distribute the packet if it has all players from the match config,
        // - unless the match is already ongoing, then only the bot players are required (humans may leave).
        bool inactive =
            context.GameState.MatchPhase is MatchPhase.Inactive or MatchPhase.Ended;
        int botCount = config.PlayerConfigurations.Count(p =>
            p.Variety.Type != PlayerClass.Human
        );
        int requiredPlayers = inactive ? config.PlayerConfigurations.Count : botCount;
        // Assumption: Bots are always the lower indexes
        for (uint i = 0; i < requiredPlayers; i++)
        {
            if (!context.GameState.GameCars.ContainsKey(i))
            {
                return;
            }
        }

        var packet = context.GameState.ToFlatBuffers();
        context.Writer.TryWrite(new DistributeGamePacket(packet));
    }
}
