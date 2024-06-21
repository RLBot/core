namespace RLBotCS.Server.BridgeMessage;

internal record EndMatch() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.MatchCommandSender.AddMatchEndCommand();
        context.MatchCommandSender.Send();
    }
}