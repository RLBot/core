using rlbot.flat;
using RLBotCS.GameControl;
using System.Net.Sockets;
using System.Threading.Channels;

namespace RLBotCS.Server.FlatbuffersMessage;

internal class ServerContext(
    Channel<IServerMessage> incomingMessages,
    MatchStarter matchStarter,
    ChannelWriter<IBridgeMessage> bridge)
{
    public TcpListener? Server { get; set; }
    public ChannelReader<IServerMessage> IncomingMessages { get; } = incomingMessages.Reader;
    public ChannelWriter<IServerMessage> IncomingMessagesWriter { get; } = incomingMessages.Writer;
    public Dictionary<int, (ChannelWriter<SessionMessage> writer, Thread thread)> Sessions { get; } = [];

    public FieldInfoT? FieldInfo { get; set; }
    public bool ShouldUpdateFieldInfo { get; set; }
    public List<ChannelWriter<MatchSettingsT>> MatchSettingsWriters { get; } = [];
    public List<ChannelWriter<FieldInfoT>> FieldInfoWriters { get; } = [];

    public MatchStarter MatchStarter { get; } = matchStarter;
    public ChannelWriter<IBridgeMessage> Bridge { get; } = bridge;

    public void StopSessions()
    {
        // Send stop message to all sessions
        foreach (var (writer, _) in Sessions.Values)
            writer.TryComplete();

        // Ensure all sessions are stopped
        foreach (var (_, thread) in Sessions.Values)
            thread.Join();

        Sessions.Clear();
    }
}