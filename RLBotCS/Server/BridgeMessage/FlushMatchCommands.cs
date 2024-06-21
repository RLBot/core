namespace RLBotCS.Server.BridgeMessage;

internal record FlushMatchCommands() : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        if (!context.QueuedMatchCommands)
            return;

        context.MatchCommandSender.Send();
        context.DelayMatchCommandSend = false;
        context.QueuedMatchCommands = false;
    }
}