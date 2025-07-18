using System.Diagnostics;
using RLBot.Flat;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.ServerMessage;

readonly struct StartMatch(MatchConfigurationT MatchConfig) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        Debug.Assert(ConfigValidator.Validate(MatchConfig));

        context.Bridge.TryWrite(new ClearRenders());

        foreach (var (writer, _) in context.Sessions.Values)
            writer.TryWrite(new SessionMessage.StopMatch(false));

        context.LastTickPacket = null;
        context.FieldInfo = null; // BridgeHandler decides if we reuse the FieldInfo
        context.MatchConfig = MatchConfig;

        context.RenderingIsEnabled = MatchConfig.EnableRendering;
        context.StateSettingIsEnabled = MatchConfig.EnableStateSetting;

        context.Bridge.TryWrite(new BridgeMessage.StartMatch(MatchConfig));

        BallPredictor.UpdateMode(MatchConfig);

        bool defaultRendering = context.RenderingIsEnabled switch
        {
            DebugRendering.OnByDefault => true,
            _ => false,
        };

        // update all sessions with the new rendering and state setting settings
        foreach (var (writer, _) in context.Sessions.Values)
        {
            SessionMessage render = new SessionMessage.RendersAllowed(defaultRendering);
            writer.TryWrite(render);

            SessionMessage stateSetting = new SessionMessage.StateSettingAllowed(
                context.StateSettingIsEnabled
            );
            writer.TryWrite(stateSetting);
        }

        // Distribute the match settings to all waiting sessions
        foreach (var writer in context.WaitingMatchConfigRequests)
        {
            writer.TryWrite(new SessionMessage.MatchConfig(MatchConfig));
        }

        context.WaitingMatchConfigRequests.Clear();

        return ServerAction.Continue;
    }
}
