namespace RLBotCS.Server.BridgeMessage;

internal interface IBridgeMessage
{
    public void HandleMessage(BridgeContext context);
}