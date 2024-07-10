using System.Net.Sockets;
using System.Threading.Channels;
using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Server.FlatbuffersMessage;

internal class ServerContext(
    Channel<IServerMessage, IServerMessage> incomingMessages,
    MatchStarter matchStarter,
    ChannelWriter<IBridgeMessage> bridge
)
{
    public TcpListener? Server { get; init; }
    public ChannelReader<IServerMessage> IncomingMessages { get; } = incomingMessages.Reader;
    public ChannelWriter<IServerMessage> IncomingMessagesWriter { get; } = incomingMessages.Writer;
    public Dictionary<int, (ChannelWriter<SessionMessage> writer, Thread thread)> Sessions { get; } = [];

    public FieldInfoT? FieldInfo { get; set; }
    public bool ShouldUpdateFieldInfo { get; set; }
    public List<ChannelWriter<SessionMessage>> MatchSettingsWriters { get; } = [];
    public List<ChannelWriter<SessionMessage>> FieldInfoWriters { get; } = [];

    public MatchStarter MatchStarter { get; } = matchStarter;
    public ChannelWriter<IBridgeMessage> Bridge { get; } = bridge;

    public PredictionMode PredictionMode { get; set; }
    public bool StateSettingIsEnabled = false;
    public bool RenderingIsEnabled = false;
}
