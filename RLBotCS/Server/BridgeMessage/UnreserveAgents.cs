namespace RLBotCS.Server.BridgeMessage;

readonly struct UnreserveAgents(int clientId) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) =>
        context.AgentMapping.UnreserveAgents(clientId);
}
