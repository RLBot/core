using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Server.BridgeMessage;

record PlayerInfoRequest(
    ChannelWriter<SessionMessage> SessionWriter,
    MatchConfigurationT MatchConfig,
    string AgentId
) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        bool foundAsBot = false;
        bool isHivemind = false;

        foreach (var player in MatchConfig.PlayerConfigurations)
        {
            if (player.AgentId == AgentId)
            {
                foundAsBot = true;
                isHivemind = player.Hivemind;
                break;
            }
        }

        if (foundAsBot && isHivemind)
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
            return;
        }

        if (foundAsBot)
        {
            if (context.AgentReservation.ReservePlayer(AgentId) is { } player)
            {
                SessionWriter.TryWrite(
                    new SessionMessage.PlayerIdPairs(player.Item2, new() { player.Item1 })
                );
            }
            return;
        }

        // Must be a script then
        for (var i = 0; i < MatchConfig.ScriptConfigurations.Count; i++)
        {
            var script = MatchConfig.ScriptConfigurations[i];
            if (script.AgentId == AgentId)
            {
                PlayerIdPair player = new() { Index = (uint)i, SpawnId = script.SpawnId };
                SessionWriter.TryWrite(new SessionMessage.PlayerIdPairs(2, new() { player }));
                return;
            }
        }

        context.Logger.LogError($"Failed to reserve bot/script with agent id {AgentId}");
    }
}
