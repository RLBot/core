namespace RLBotCS.Server.FlatbuffersMessage;

internal record SessionReady() : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.MatchStarter.IncrementConnectionReadies();

        return ServerAction.Continue;
    }
}
