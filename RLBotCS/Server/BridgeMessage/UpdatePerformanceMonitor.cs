using RLBot.Flat;

namespace RLBotCS.Server.BridgeMessage;

readonly struct UpdatePerformanceMonitor(PerformanceMonitor Mode) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.PerfMonitor.SetDisplayMode(Mode);
    }
}
