using rlbot.flat;
using System.Threading.Channels;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record IntroDataRequest(
    ChannelWriter<MatchSettingsT> MatchSettingsWriter,
    ChannelWriter<FieldInfoT> FieldInfoWriter) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        if (context.MatchStarter.GetMatchSettings() is { } settings)
        {
            MatchSettingsWriter.TryWrite(settings);
            MatchSettingsWriter.TryComplete();
        }
        else
            context.MatchSettingsWriters.Add(MatchSettingsWriter);

        if (context.FieldInfo != null)
        {
            FieldInfoWriter.TryWrite(context.FieldInfo);
            FieldInfoWriter.TryComplete();
        }
        else
            context.FieldInfoWriters.Add(FieldInfoWriter);

        return ServerAction.Continue;
    }
}