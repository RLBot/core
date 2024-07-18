using rlbot.flat;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record SendMatchComm(int ClientId, MatchCommT MatchComm) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        var message = new SessionMessage.MatchComm(MatchComm);

        foreach (var session in context.Sessions)
        {
            var id = session.Key;
            var writer = session.Value;

            // Don't send the message back to the client that sent it.
            if (id != ClientId)
            {
                writer.writer.TryWrite(message);
            }
        }

        return ServerAction.Continue;
    }
}
