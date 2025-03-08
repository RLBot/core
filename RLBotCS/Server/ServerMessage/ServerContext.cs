using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.ServerMessage;

class ServerContext(
    Channel<IServerMessage, IServerMessage> incomingMessages,
    MatchStarter matchStarter,
    ChannelWriter<IBridgeMessage> bridge
)
{
    public readonly ILogger Logger = Logging.GetLogger("FlatBuffersServer");

    public TcpListener? Server { get; init; }
    public ChannelReader<IServerMessage> IncomingMessages { get; } = incomingMessages.Reader;
    public ChannelWriter<IServerMessage> IncomingMessagesWriter { get; } =
        incomingMessages.Writer;
    public Dictionary<
        int,
        (ChannelWriter<SessionMessage> writer, Thread thread, int spawnId)
    > Sessions { get; } = [];

    public FieldInfoT? FieldInfo { get; set; }
    public bool ShouldUpdateFieldInfo { get; set; }
    
    /// <summary>List of sessions that have yet to receive the match config (and their indexes).
    /// We clear the list once they have been notified.</summary>
    public List<(ChannelWriter<SessionMessage>, string)> MatchConfigWriters { get; } = [];
    
    /// <summary>List of sessions that have yet to receive the field info.
    /// We clear the list once they have been notified.</summary>
    public List<ChannelWriter<SessionMessage>> FieldInfoWriters { get; } = [];

    public MatchStarter MatchStarter { get; } = matchStarter;
    public ChannelWriter<IBridgeMessage> Bridge { get; } = bridge;

    public bool StateSettingIsEnabled = false;
    public bool RenderingIsEnabled = false;

    public GamePacketT? LastTickPacket { get; set; }
}
