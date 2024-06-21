namespace RLBotCS.Server.BridgeMessage;

internal record ClearRenders() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.QuickChat.ClearChats();
        context.PerfMonitor.ClearAll();
        context.RenderingMgmt.ClearAllRenders(context.MatchCommandSender);
    }
}