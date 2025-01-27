namespace RLBotCS.Server.FlatbuffersMessage;

record MapSpawned(string MapName) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.FieldInfo = null;
        context.ShouldUpdateFieldInfo = true;
        context.MatchStarter.MapSpawned(MapName);

        return ServerAction.Continue;
    }
}
