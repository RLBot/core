namespace RLBotCS.Server.BridgeMessage;

internal record MarkQueuingComplete() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.QueuingCommandsComplete = true;
    }
}