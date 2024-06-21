using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.Conversion;

namespace RLBotCS.Server.BridgeMessage;

internal record SpawnMap(MatchConfigurationT matchConfig) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.MatchHasStarted = false;
        context.DelayMatchCommandSend = true;

        string loadMapCommand = FlatToCommand.MakeOpenCommand(matchConfig);
        context.Logger.LogInformation($"Starting match with command: {loadMapCommand}");

        context.MatchCommandSender.AddConsoleCommand(loadMapCommand);
        context.MatchCommandSender.Send();
    }
}