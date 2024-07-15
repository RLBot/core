using System.Threading.Channels;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record IntroDataRequest(ChannelWriter<SessionMessage> sessionWriter) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (context.MatchStarter.GetMatchSettings() is { } settings)
            sessionWriter.TryWrite(new SessionMessage.MatchSettings(settings));
        else
            context.MatchSettingsWriters.Add(sessionWriter);

        if (context.FieldInfo != null)
        {
            sessionWriter.TryWrite(new SessionMessage.FieldInfo(context.FieldInfo));
        }
        else
            context.FieldInfoWriters.Add(sessionWriter);

        return ServerAction.Continue;
    }
}
