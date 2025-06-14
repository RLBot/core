using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBotCS.Server.BridgeMessage;

record AgentReservationRequest(
    int ClientId,
    ChannelWriter<SessionMessage> SessionWriter,
    string AgentId
) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        var matchConfig = context.MatchConfig;
        if (matchConfig == null)
        {
            context.WaitingAgentRequests.Add(this);
            return;
        }

        bool isHivemind = false;

        foreach (var player in matchConfig.PlayerConfigurations)
        {
            if (player.Variety.Value is CustomBotT bot && bot.AgentId == AgentId)
            {
                isHivemind = bot.Hivemind;
                break;
            }
        }

        if (isHivemind)
        {
            if (context.AgentMapping.ReserveAgents(ClientId, AgentId) is var (players, team))
            {
                SessionWriter.TryWrite(new SessionMessage.PlayerIdPairs(team, players));
                return;
            }
        }
        else
        {
            if (context.AgentMapping.ReserveAgent(ClientId, AgentId) is var (player, team))
            {
                SessionWriter.TryWrite(
                    new SessionMessage.PlayerIdPairs(team, new() { player })
                );
                return;
            }
        }

        context.Logger.LogError($"Failed to reserve bot/script with agent id {AgentId}");
    }
}
