namespace RLBotCS.Server.BridgeMessage;

readonly struct EndMatch() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.MatchCommandQueue.AddMatchEndCommand();
        context.MatchCommandQueue.Flush();
        context.MatchStarter.ResetMatchStarting();
    }
}
