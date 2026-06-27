using RLBot.Flat;

namespace RLBotCS.Server.ServerMessage;

readonly struct UpdateRenderingStatus(RenderingStatusT Status) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        // Ignore all messages if rendering is force-disabled
        if (context.RenderingIsEnabled == DebugRendering.AlwaysOff)
            return ServerAction.Continue;

        SessionMessage.UpdateRenderingStatus message = new(Status);

        // Distribute to all sessions;
        // they will figure out on their own if rendering should be enable/disabled
        foreach (var (_, (writer, _)) in context.Sessions)
        {
            writer.TryWrite(message);
        }

        return ServerAction.Continue;
    }
}
