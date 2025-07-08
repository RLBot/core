namespace RLBotCS.Server.BridgeMessage;

readonly struct ClearRenders() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.QuickChat.ClearChats();
        context.PerfMonitor.ClearAll();
        context.RenderingMgmt.ClearAllRenders();
    }
}
