using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.ServerMessage;

class ServerContext(
    Channel<IServerMessage, IServerMessage> incomingMessages,
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
        (ChannelWriter<SessionMessage> writer, Thread thread)
    > Sessions { get; } = [];

    public FieldInfoT? FieldInfo { get; set; }
    public bool ShouldUpdateFieldInfo { get; set; } // TODO: This bool might be redundant

    /// <summary>List of sessions that have yet to receive the match config.
    /// We clear the list once they have been notified.</summary>
    public List<ChannelWriter<SessionMessage>> WaitingMatchConfigRequests { get; } = [];

    /// <summary>List of sessions that have yet to receive the field info.
    /// We clear the list once they have been notified.</summary>
    public List<ChannelWriter<SessionMessage>> WaitingFieldInfoRequests { get; } = [];

    public ChannelWriter<IBridgeMessage> Bridge { get; } = bridge;

    public bool StateSettingIsEnabled = false;
    public bool RenderingIsEnabled = false;

    public GamePacketT? LastTickPacket { get; set; }
    public MatchConfigurationT? MatchConfig { get; set; }
}
