namespace RLBotCS.Server.FlatbuffersMessage;

internal record StopMatch(bool ShutdownServer) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.MatchStarter.NullMatchSettings();
        context.FieldInfo = null;
        context.ShouldUpdateFieldInfo = false;

        if (ShutdownServer)
        {
            context.IncomingMessagesWriter.TryComplete();
            return ServerAction.Stop;
        }

        context.StopSessions();
        return ServerAction.Continue;
    }
}