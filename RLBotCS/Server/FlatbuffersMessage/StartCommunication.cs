namespace RLBotCS.Server.FlatbuffersMessage;

record StartCommunication : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.MatchStarter.StartCommunication();
        return ServerAction.Continue;
    }
}
