namespace RLBotCS.Server.FlatbuffersMessage;

internal record StopMatch(bool ShutdownServer) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.FieldInfo = null;
        context.ShouldUpdateFieldInfo = false;

        if (ShutdownServer)
        {
            context.IncomingMessagesWriter.TryComplete();
            return ServerAction.Stop;
        }

        foreach (var (writer, _) in context.Sessions.Values)
            writer.TryWrite(new SessionMessage.StopMatch());
        return ServerAction.Continue;
    }
}
