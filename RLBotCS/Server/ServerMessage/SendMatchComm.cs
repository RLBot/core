using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBotCS.Server.ServerMessage;

record SendMatchComm(int ClientId, MatchCommT MatchComm) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        SessionMessage.MatchComm message = new(MatchComm);

        if (context.LastTickPacket is null)
        {
            context.Logger.LogWarning("Received MatchComm before receiving a GameTickPacket.");
            return ServerAction.Continue;
        }

        foreach (var (id, (writer, _)) in context.Sessions)
        {
            // Don't send the message back to the client that sent it.
            if (id == ClientId)
                continue;

            writer.TryWrite(message);
        }

        return ServerAction.Continue;
    }
}
