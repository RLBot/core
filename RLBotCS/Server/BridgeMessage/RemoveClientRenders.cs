namespace RLBotCS.Server.BridgeMessage;

record RemoveClientRenders(int ClientId) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) =>
        context.RenderingMgmt.ClearClientRenders(ClientId);
}