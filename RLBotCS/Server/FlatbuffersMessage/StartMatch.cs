using rlbot.flat;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record StartMatch(MatchSettingsT MatchSettings) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.RenderingIsEnabled = MatchSettings.EnableRendering;
        context.StateSettingIsEnabled = MatchSettings.EnableStateSetting;

        context.MatchStarter.StartMatch(MatchSettings);
        context.BallPredictor.Sync(MatchSettings);

        // update all sessions with the new rendering and state setting settings
        foreach (var (writer, _) in context.Sessions.Values)
        {
            SessionMessage render = new SessionMessage.RendersAllowed(MatchSettings.EnableRendering);
            writer.TryWrite(render);

            SessionMessage stateSetting = new SessionMessage.StateSettingAllowed(MatchSettings.EnableStateSetting);
            writer.TryWrite(stateSetting);
        }

        // Distribute the match settings to all waiting sessions
        foreach (var writer in context.MatchSettingsWriters)
        {
            writer.TryWrite(MatchSettings);
            writer.TryComplete();
        }

        context.MatchSettingsWriters.Clear();

        return ServerAction.Continue;
    }
}