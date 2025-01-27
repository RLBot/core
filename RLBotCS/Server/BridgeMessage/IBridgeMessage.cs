namespace RLBotCS.Server.BridgeMessage;

interface IBridgeMessage
{
    public void HandleMessage(BridgeContext context);
}