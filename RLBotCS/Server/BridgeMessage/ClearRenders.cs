namespace RLBotCS.Server.BridgeMessage;

record ClearRenders() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.QuickChat.ClearChats();
        context.PerfMonitor.ClearAll();
        context.RenderingMgmt.ClearAllRenders(context.MatchCommandSender);
    }
}
