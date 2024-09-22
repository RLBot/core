namespace RLBotCS.Server.FlatbuffersMessage;

internal record SessionReady(bool incrConnections, int ClientId, int SpawnId) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (incrConnections)
            context.MatchStarter.IncrementConnectionReadies();

        var (writer, session, _) = context.Sessions[ClientId];
        context.Sessions[ClientId] = (writer, session, SpawnId);

        return ServerAction.Continue;
    }
}
