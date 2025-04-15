namespace RLBotCS.Server.BridgeMessage;

record UnreserveAgents(int clientId) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) =>
        context.AgentMapping.UnreserveAgents(clientId);
}
