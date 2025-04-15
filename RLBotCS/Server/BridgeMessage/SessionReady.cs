namespace RLBotCS.Server.BridgeMessage;

record SessionReady(bool incrConnections) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        // TODO
        if (incrConnections)
            context.MatchStarter.IncrementConnectionReady();
    }
}
