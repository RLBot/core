namespace RLBotCS.Server.FlatbuffersMessage;

internal record StopMatch(bool ShutdownServer) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (ShutdownServer)
        {
            context.IncomingMessagesWriter.TryComplete();
            return ServerAction.Stop;
        }

        context.MatchStarter.SetNullMatchSettings();
        context.FieldInfo = null;
        context.ShouldUpdateFieldInfo = false;
        context.LastTickPacket = null;
        context.Bridge.TryWrite(new EndMatch());

        foreach (var (writer, _, _) in context.Sessions.Values)
            writer.TryWrite(new SessionMessage.StopMatch(ShutdownServer));

        return ServerAction.Continue;
    }
}
