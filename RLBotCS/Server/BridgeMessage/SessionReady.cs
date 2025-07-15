namespace RLBotCS.Server.BridgeMessage;

readonly struct SessionReady(int ClientId) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.AgentMapping.ReadyAgents(ClientId);
        context.MatchStarter.CheckAgentReadyStatus(context.GetPlayerSpawner());
    }
}
