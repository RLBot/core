using Microsoft.Extensions.Logging;
using rlbot.flat;

namespace RLBotCS.Server.FlatbuffersMessage;

record SendMatchComm(int ClientId, int SpawnId, MatchCommT MatchComm) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        SessionMessage.MatchComm message = new(MatchComm);

        if (context.LastTickPacket is null)
        {
            context.Logger.LogWarning("Received MatchComm before receiving a GameTickPacket.");
            return ServerAction.Continue;
        }

        foreach (var (id, (writer, _, spawnId)) in context.Sessions)
        {
            // Don't send the message back to the client that sent it.
            if (id == ClientId)
                continue;

            // intentional let spawnId == 0 pass through
            // this should allow special connections like match managers to receive all messages
            if (message.Message.TeamOnly && spawnId != 0)
            {
                // team 0 is blue, and 1 is orange
                // but team 2 means only scripts (so, not players) should receive the message

                var player = context.LastTickPacket.Players.Find(player =>
                    player.SpawnId == spawnId
                );
                if (message.Message.Team == 2)
                {
                    if (player is not null)
                        continue;
                }
                else if (player is null || player.Team != message.Message.Team)
                    continue;
            }

            writer.TryWrite(message);
        }

        return ServerAction.Continue;
    }
}
