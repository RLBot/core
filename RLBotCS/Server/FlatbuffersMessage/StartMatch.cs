using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record StartMatch(MatchSettingsT MatchSettings) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.LastTickPacket = null;
        context.Bridge.TryWrite(new ClearRenders());

        foreach (var (writer, _, _) in context.Sessions.Values)
            writer.TryWrite(new SessionMessage.StopMatch(false));

        if (MatchSettings.MutatorSettings == null)
            MatchSettings.MutatorSettings = new();

        context.RenderingIsEnabled = MatchSettings.EnableRendering;
        context.StateSettingIsEnabled = MatchSettings.EnableStateSetting;

        context.MatchStarter.StartMatch(MatchSettings);

        var newMode = BallPredictor.GetMode(MatchSettings);
        if (newMode != context.PredictionMode)
        {
            BallPredictor.SetMode(newMode);
            context.PredictionMode = newMode;
        }

        // update all sessions with the new rendering and state setting settings
        foreach (var (writer, _, _) in context.Sessions.Values)
        {
            SessionMessage render = new SessionMessage.RendersAllowed(
                MatchSettings.EnableRendering
            );
            writer.TryWrite(render);

            SessionMessage stateSetting = new SessionMessage.StateSettingAllowed(
                MatchSettings.EnableStateSetting
            );
            writer.TryWrite(stateSetting);
        }

        // Distribute the match settings to all waiting sessions
        foreach (var writer in context.MatchSettingsWriters)
        {
            writer.TryWrite(new SessionMessage.MatchSettings(MatchSettings));
        }

        context.MatchSettingsWriters.Clear();

        return ServerAction.Continue;
    }
}
