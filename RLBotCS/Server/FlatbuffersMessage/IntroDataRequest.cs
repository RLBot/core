using System.Threading.Channels;
using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record IntroDataRequest(ChannelWriter<SessionMessage> SessionWriter, string AgentId)
    : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (context.MatchStarter.GetMatchConfig() is { } matchConfig)
        {
            SessionWriter.TryWrite(new SessionMessage.MatchConfig(matchConfig));

            if (AgentId != string.Empty)
                context.Bridge.TryWrite(
                    new PlayerInfoRequest(SessionWriter, matchConfig, AgentId)
                );
        }
        else
            context.MatchConfigWriters.Add((SessionWriter, AgentId));

        if (context.FieldInfo != null)
            SessionWriter.TryWrite(new SessionMessage.FieldInfo(context.FieldInfo));
        else
            context.FieldInfoWriters.Add(SessionWriter);

        return ServerAction.Continue;
    }
}
