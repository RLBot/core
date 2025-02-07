using rlbot.flat;

namespace RLBotCS.Server.BridgeMessage;

record ClearProcessPlayerReservation(MatchConfigurationT MatchConfig) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) =>
        context.AgentReservation.SetPlayers(MatchConfig);
}
