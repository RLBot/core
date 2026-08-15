using RLBot.Flat;

namespace RLBotCS.Server.BridgeMessage;

readonly struct RunConsoleCommand(ConsoleCommandT command) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.MatchCommandQueue.AddConsoleCommand(command.Command);
    }
}
