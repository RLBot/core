using rlbot.flat;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.FlatbuffersMessage;

record StartMatch(MatchConfigurationT MatchConfig) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.LastTickPacket = null;
        context.Bridge.TryWrite(new ClearRenders());

        foreach (var (writer, _, _) in context.Sessions.Values)
            writer.TryWrite(new SessionMessage.StopMatch(false));

        if (MatchConfig.Mutators == null)
            MatchConfig.Mutators = new();

        context.RenderingIsEnabled = MatchConfig.EnableRendering;
        context.StateSettingIsEnabled = MatchConfig.EnableStateSetting;

        context.MatchStarter.StartMatch(MatchConfig);
        var realMatchConfig = context.MatchStarter.GetMatchConfig() ?? MatchConfig;
        context.Bridge.TryWrite(new ClearProcessPlayerReservation(realMatchConfig));

        var newMode = BallPredictor.GetMode(realMatchConfig);
        if (newMode != context.PredictionMode)
        {
            BallPredictor.SetMode(newMode);
            context.PredictionMode = newMode;
        }

        // update all sessions with the new rendering and state setting settings
        foreach (var (writer, _, _) in context.Sessions.Values)
        {
            SessionMessage render = new SessionMessage.RendersAllowed(
                context.RenderingIsEnabled
            );
            writer.TryWrite(render);

            SessionMessage stateSetting = new SessionMessage.StateSettingAllowed(
                context.StateSettingIsEnabled
            );
            writer.TryWrite(stateSetting);
        }

        // Distribute the match settings to all waiting sessions
        foreach (var (writer, agentId) in context.MatchConfigWriters)
        {
            writer.TryWrite(new SessionMessage.MatchConfig(realMatchConfig));

            if (agentId != string.Empty)
                context.Bridge.TryWrite(
                    new PlayerInfoRequest(writer, realMatchConfig, agentId)
                );
        }

        context.MatchConfigWriters.Clear();

        return ServerAction.Continue;
    }
}
