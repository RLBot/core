using RLBot.Flat;

namespace RLBotCS.Server.ServerMessage;

readonly struct UpdatePerformanceMonitor(PerformanceMonitor Mode) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (context.MatchConfig is { } matchConfig)
            matchConfig.PerformanceMonitor = Mode;

        context.Bridge.TryWrite(new BridgeMessage.UpdatePerformanceMonitor(Mode));

        return ServerAction.Continue;
    }
}
