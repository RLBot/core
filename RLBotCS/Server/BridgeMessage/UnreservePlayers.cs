using RLBotCS.ManagerTools;

namespace RLBotCS.Server.BridgeMessage;

internal record UnreservePlayers(uint team, List<PlayerIdPair> players) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) =>
        context.AgentReservation.UnreservePlayers(team, players);
}