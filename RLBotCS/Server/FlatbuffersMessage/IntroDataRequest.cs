using System.Threading.Channels;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record IntroDataRequest(ChannelWriter<SessionMessage> SessionWriter, string GroupId)
    : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (context.MatchStarter.GetMatchSettings() is { } settings)
        {
            SessionWriter.TryWrite(new SessionMessage.MatchSettings(settings));
            context.Bridge.TryWrite(new PlayerInfoRequest(SessionWriter, settings, GroupId));
        }
        else
            context.MatchSettingsWriters.Add((SessionWriter, GroupId));

        if (context.FieldInfo != null)
            SessionWriter.TryWrite(new SessionMessage.FieldInfo(context.FieldInfo));
        else
            context.FieldInfoWriters.Add(SessionWriter);

        return ServerAction.Continue;
    }
}
