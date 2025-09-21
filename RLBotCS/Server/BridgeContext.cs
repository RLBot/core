using System.Threading.Channels;
using Bridge.Controller;
using Bridge.State;
using Bridge.TCP;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;
using RLBotCS.Server.ServerMessage;

namespace RLBotCS.Server;

class BridgeContext(
    ChannelWriter<IServerMessage> writer,
    ChannelReader<IBridgeMessage> reader,
    TcpMessenger messenger,
    MatchStarter matchStarter
)
{
    public readonly ILogger Logger = Logging.GetLogger("BridgeHandler");

    public int ticksSkipped = 0;
    public uint ticksSinceMapLoad = 0;
    public GameState GameState = new();
    public MatchStarter MatchStarter { get; } = matchStarter;
    public MatchConfigurationT? MatchConfig => MatchStarter.GetMatchConfig();
    public AgentMapping AgentMapping => MatchStarter.AgentMapping;

    /// <summary>List of messages that wants to reserve an agent id once
    /// bridge receives the new match config. Cleared afterward.</summary>
    public List<AgentReservationRequest> WaitingAgentRequests = new();

    /// <summary>List of messages that wants to set their agent's loadout once
    /// bridge receives the new match config. Cleared afterward.</summary>
    public List<SetInitLoadout> WaitingInitLoadouts = new();

    public ChannelWriter<IServerMessage> Writer { get; } = writer;
    public ChannelReader<IBridgeMessage> Reader { get; } = reader;
    public TcpMessenger Messenger { get; } = messenger;
    public MatchCommandQueue MatchCommandQueue { get; } = new(messenger);
    public SpawnCommandQueue SpawnCommandQueue { get; } = new(messenger);
    public PlayerInputSender PlayerInputSender { get; } = new(messenger);
    public Rendering RenderingMgmt { get; } = new(messenger);
    public QuickChat QuickChat { get; } = new();
    public PerfMonitor PerfMonitor { get; } = new();

    public PlayerSpawner GetPlayerSpawner() => new(ref GameState, SpawnCommandQueue);

    public void UpdateTimeMutators()
    {
        var mutators = MatchConfig!.Mutators;

        GameState.GameTimeRemaining = mutators.MatchLength switch
        {
            MatchLengthMutator.FiveMinutes => 5 * 60,
            MatchLengthMutator.TenMinutes => 10 * 60,
            MatchLengthMutator.TwentyMinutes => 20 * 60,
            MatchLengthMutator.Unlimited => 0,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mutators.MatchLength),
                mutators.MatchLength,
                null
            ),
        };

        GameState.MatchLength = mutators.MatchLength switch
        {
            MatchLengthMutator.FiveMinutes => Bridge.Packet.MatchLength.FiveMinutes,
            MatchLengthMutator.TenMinutes => Bridge.Packet.MatchLength.TenMinutes,
            MatchLengthMutator.TwentyMinutes => Bridge.Packet.MatchLength.TwentyMinutes,
            MatchLengthMutator.Unlimited => Bridge.Packet.MatchLength.Unlimited,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mutators.MatchLength),
                mutators.MatchLength,
                null
            ),
        };

        GameState.RespawnTime = mutators.RespawnTime switch
        {
            RespawnTimeMutator.ThreeSeconds => 3,
            RespawnTimeMutator.TwoSeconds => 2,
            RespawnTimeMutator.OneSecond => 1,
            RespawnTimeMutator.DisableGoalReset => 3,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mutators.RespawnTime),
                mutators.RespawnTime,
                null
            ),
        };
    }
}
