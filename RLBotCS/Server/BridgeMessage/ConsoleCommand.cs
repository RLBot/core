namespace RLBotCS.Server.BridgeMessage;

record ConsoleCommand(string Command) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) => context.QueueConsoleCommand(Command);
}