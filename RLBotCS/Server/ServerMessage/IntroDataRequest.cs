using System.Threading.Channels;
using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.ServerMessage;

/// <summary>
/// Fetch match config, field info, and relevant bot indexes for a client.
/// </summary>
record IntroDataRequest(ChannelWriter<SessionMessage> SessionWriter, string AgentId)
    : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (context.MatchConfig is { } matchConfig)
        {
            SessionWriter.TryWrite(new SessionMessage.MatchConfig(matchConfig));

            if (AgentId != string.Empty)
                context.Bridge.TryWrite(
                    new PlayerInfoRequest(SessionWriter, matchConfig, AgentId)
                );
        }
        else
        {
            // Notify the client when it arrives
            context.MatchConfigWriters.Add((SessionWriter, AgentId));
        }

        if (context.FieldInfo != null)
        {
            SessionWriter.TryWrite(new SessionMessage.FieldInfo(context.FieldInfo));
        }
        else
        {
            // Notify the client when it arrives
            context.FieldInfoWriters.Add(SessionWriter);
        }

        return ServerAction.Continue;
    }
}
