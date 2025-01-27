namespace RLBotCS.Server.BridgeMessage;

record EndMatch() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.MatchCommandSender.AddMatchEndCommand();
        context.MatchCommandSender.Send();
    }
}