using rlbot.flat;

namespace RLBotCS.Server.BridgeMessage;

internal record ClearProcessPlayerReservation(MatchConfigurationT MatchConfig) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) =>
        context.AgentReservation.SetPlayers(MatchConfig);
}