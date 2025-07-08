namespace RLBotCS.Server.BridgeMessage;

readonly struct RemoveClientRenders(int ClientId) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) =>
        context.RenderingMgmt.ClearClientRenders(ClientId);
}
