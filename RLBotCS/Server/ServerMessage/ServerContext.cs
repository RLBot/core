using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
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

    /// <summary>List of sessions that have yet to receive the match config.
    /// We clear the list once they have been notified.</summary>
    public List<ChannelWriter<SessionMessage>> WaitingMatchConfigRequests { get; } = [];

    /// <summary>List of sessions that have yet to receive the field info.
    /// We clear the list once they have been notified.</summary>
    public List<ChannelWriter<SessionMessage>> WaitingFieldInfoRequests { get; } = [];

    public ChannelWriter<IBridgeMessage> Bridge { get; } = bridge;

    public bool StateSettingIsEnabled = false;
    public DebugRendering RenderingIsEnabled = DebugRendering.OffByDefault;

    public GamePacketT? LastTickPacket { get; set; }

    /// <summary>The MatchConfig for the latest started match.
    /// Note that this config is not necessarily identical to the one at BridgeHandler.
    /// This one is the original validated config from the client.
    /// The BridgeHandler's config may contain updated names, e.g. "Nexto (2)",
    /// and updated loadouts.</summary>
    public MatchConfigurationT? MatchConfig { get; set; }

    public readonly Dictionary<int, uint> MissedMessagesCount = new();

    public void DistributeMessage(SessionMessage message)
    {
        foreach (var (id, (writer, _)) in Sessions)
        {
            if (!writer.TryWrite(message))
            {
                uint missedMessages = MissedMessagesCount.GetValueOrDefault(id);
                missedMessages += 1;
                MissedMessagesCount[id] = missedMessages;

                // Log warning with exponential backoff,
                // e.g. every 10, 100, 1000, etc. messages missed
                // (including the first missed message)
                uint powerOfTen = (uint)Math.Pow(10, Math.Ceiling(Math.Log10(missedMessages)));
                if (missedMessages == powerOfTen)
                {
                    Logger.LogWarning(
                        "Session {}'s outbound message queue is full ({} messages missed)",
                        id,
                        missedMessages
                    );
                }
            }
        }
    }
}
