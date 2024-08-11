namespace RLBotCS.Server.FlatbuffersMessage;

internal record StopMatch(bool ShutdownServer) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.MatchStarter.SetNullMatchSettings();
        context.FieldInfo = null;
        context.ShouldUpdateFieldInfo = false;

        if (ShutdownServer)
        {
            context.IncomingMessagesWriter.TryComplete();
            return ServerAction.Stop;
        }

        context.Bridge.TryWrite(new EndMatch());
        foreach (var (writer, _) in context.Sessions.Values)
            writer.TryWrite(new SessionMessage.StopMatch(ShutdownServer));
        return ServerAction.Continue;
    }
}
