namespace RLBotCS.Server.FlatbuffersMessage;

internal record MapSpawned(bool NotifyMatchStarter) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.FieldInfo = null;
        context.ShouldUpdateFieldInfo = true;
        context.MatchStarter.MapSpawned();

        return ServerAction.Continue;
    }
}
