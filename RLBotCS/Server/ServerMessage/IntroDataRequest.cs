using System.Threading.Channels;
using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.ServerMessage;

/// <summary>
/// Fetch match config, field info, and relevant bot indexes for a client.
/// </summary>
readonly struct IntroDataRequest(
    int ClientId,
    ChannelWriter<SessionMessage> SessionWriter,
    string AgentId
) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (context.MatchConfig is { } matchConfig)
        {
            SessionWriter.TryWrite(new SessionMessage.MatchConfig(matchConfig));
        }
        else
        {
            // Notify the client when it arrives
            context.WaitingMatchConfigRequests.Add(SessionWriter);
        }

        if (AgentId != "")
        {
            context.Bridge.TryWrite(
                new AgentReservationRequest(ClientId, SessionWriter, AgentId)
            );
        }

        if (context.FieldInfo != null)
        {
            SessionWriter.TryWrite(new SessionMessage.FieldInfo(context.FieldInfo));
        }
        else
        {
            // Notify the client when it arrives
            context.WaitingFieldInfoRequests.Add(SessionWriter);
        }

        return ServerAction.Continue;
    }
}
