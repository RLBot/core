using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Server.BridgeMessage;

internal record PlayerInfoRequest(
    ChannelWriter<SessionMessage> SessionWriter,
    MatchConfigurationT MatchConfig,
    string AgentId
) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        bool isHivemind = false;
        bool isScript = true;

        foreach (var player in MatchConfig.PlayerConfigurations)
        {
            if (player.AgentId == AgentId)
            {
                isScript = false;
                isHivemind = player.Hivemind;
                break;
            }
        }

        if (isHivemind)
        {
            if (context.AgentReservation.ReservePlayers(AgentId) is { } players)
            {
                SessionWriter.TryWrite(
                    new SessionMessage.PlayerIdPairs(players.Item2, players.Item1)
                );
            }
            else
            {
                context.Logger.LogError(
                    $"Failed to reserve players for hivemind with agent id {AgentId}"
                );
            }
        }
        else if (isScript)
        {
            for (var i = 0; i < MatchConfig.ScriptConfigurations.Count; i++)
            {
                var script = MatchConfig.ScriptConfigurations[i];
                if (script.AgentId == AgentId)
                {
                    PlayerIdPair player = new() { Index = (uint)i, SpawnId = script.SpawnId };
                    SessionWriter.TryWrite(
                        new SessionMessage.PlayerIdPairs(2, new() { player })
                    );
                    return;
                }
            }

            context.Logger.LogError($"Failed to find script with agent id {AgentId}");
        }
        else if (context.AgentReservation.ReservePlayer(AgentId) is { } player)
        {
            SessionWriter.TryWrite(
                new SessionMessage.PlayerIdPairs(player.Item2, new() { player.Item1 })
            );
        }
        else
        {
            context.Logger.LogError($"Failed to reserve player for agent id {AgentId}");
        }
    }
}