namespace RLBotCS.Server.BridgeMessage;

record EndMatch() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.MatchCommandQueue.AddMatchEndCommand();
        context.MatchCommandQueue.Flush();
        context.MatchStarter.ResetMatchStarting();
    }
}
