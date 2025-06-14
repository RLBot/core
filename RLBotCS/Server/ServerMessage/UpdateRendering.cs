using RLBot.Flat;

namespace RLBotCS.Server.ServerMessage;

record UpdateRendering(RenderingStatus Status) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        // Ignore all messages if rendering is force-disabled
        if (context.RenderingIsEnabled == DebugRendering.AlwaysOff)
            return ServerAction.Continue;

        SessionMessage.UpdateRendering message = new(Status);

        // Distribute to all sessions;
        // they will figure out on their own if rendering should be enable/disabled
        foreach (var (id, (writer, _)) in context.Sessions)
        {
            writer.TryWrite(message);
        }

        return ServerAction.Continue;
    }
}
