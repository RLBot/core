namespace RLBotCS.Server.ServerMessage;

record StartCommunication : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.MatchStarter.StartCommunication();
        return ServerAction.Continue;
    }
}
