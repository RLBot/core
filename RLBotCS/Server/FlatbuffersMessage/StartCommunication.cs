namespace RLBotCS.Server.FlatbuffersMessage;

internal record StartCommunication : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.MatchStarter.StartCommunication();
        return ServerAction.Continue;
    }
}
