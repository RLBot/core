using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.ServerMessage;

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

        context.MatchStarter.StartMatch(MatchConfig); // May modify the match config
        context.Bridge.TryWrite(new ClearProcessPlayerReservation(MatchConfig));

        BallPredictor.UpdateMode(MatchConfig);

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
            writer.TryWrite(new SessionMessage.MatchConfig(MatchConfig));

            if (agentId != string.Empty)
                context.Bridge.TryWrite(new PlayerInfoRequest(writer, MatchConfig, agentId));
        }

        context.MatchConfigWriters.Clear();

        return ServerAction.Continue;
    }
}
