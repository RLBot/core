namespace RLBotCS.Server.FlatbuffersMessage;

internal record SessionClosed(int ClientId) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.Bridge.TryWrite(new RemoveClientRenders(ClientId));
        context.Sessions.Remove(ClientId);
        Console.WriteLine("Session closed.");

        return ServerAction.Continue;
    }
}