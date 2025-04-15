﻿using System.Threading.Channels;
using Bridge.Controller;
using Bridge.State;
using Bridge.TCP;
using Microsoft.Extensions.Logging;
using rlbot.flat;
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
    public GameState GameState = new();
    public MatchStarter MatchStarter { get; } = matchStarter;
    public MatchConfigurationT? MatchConfig => MatchStarter.GetMatchConfig();
    public AgentMapping AgentMapping => MatchStarter.AgentMapping;

    public List<AgentReservationRequest> WaitingAgentRequests = new();

    public ChannelWriter<IServerMessage> Writer { get; } = writer;
    public ChannelReader<IBridgeMessage> Reader { get; } = reader;
    public TcpMessenger Messenger { get; } = messenger;
    public MatchCommandSender MatchCommandSender { get; } = new(messenger);
    public PlayerInputSender PlayerInputSender { get; } = new(messenger);
    public Rendering RenderingMgmt { get; } = new(messenger);
    public QuickChat QuickChat { get; } = new();
    public PerfMonitor PerfMonitor { get; } = new();
    
    public bool MatchHasStarted { get; set; }
    public bool QueuedMatchCommands { get; set; }
    public bool DelayMatchCommandSend { get; set; }
    public bool QueuingCommandsComplete { get; set; }

    public void QueueConsoleCommand(string command)
    {
        QueuedMatchCommands = true;
        QueuingCommandsComplete = false;
        MatchCommandSender.AddConsoleCommand(command);
    }
}
