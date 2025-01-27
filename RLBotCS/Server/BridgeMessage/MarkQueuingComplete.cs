namespace RLBotCS.Server.BridgeMessage;

record MarkQueuingComplete() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.QueuingCommandsComplete = true;
    }
}