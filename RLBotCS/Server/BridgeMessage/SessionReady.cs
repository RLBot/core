namespace RLBotCS.Server.BridgeMessage;

record SessionReady(int ClientId) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.AgentMapping.ReadyAgents(ClientId);
        context.MatchStarter.CheckAgentReadyStatus();
    }
}
