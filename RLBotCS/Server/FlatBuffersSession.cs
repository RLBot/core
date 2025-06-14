using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBotCS.ManagerTools;
using RLBotCS.Model;
using RLBotCS.Server.BridgeMessage;
using RLBotCS.Server.ServerMessage;
using StartMatch = RLBotCS.Server.ServerMessage.StartMatch;

namespace RLBotCS.Server;

/// <summary>
/// A message sent to <see cref="FlatBuffersSession"/> from <see cref="FlatBuffersServer"/>.
/// </summary>
record SessionMessage
{
    public record MatchConfig(MatchConfigurationT Config) : SessionMessage;

    public record FieldInfo(FieldInfoT Info) : SessionMessage;

    public record PlayerIdPairs(uint Team, List<PlayerIdPair> IdMaps) : SessionMessage;

    public record DistributeBallPrediction(BallPredictionT BallPrediction) : SessionMessage;

    public record DistributeGameState(GamePacketT GameState) : SessionMessage;

    public record RendersAllowed(bool Allowed) : SessionMessage;

    public record StateSettingAllowed(bool Allowed) : SessionMessage;

    public record MatchComm(MatchCommT Message) : SessionMessage;

    public record StopMatch(bool Force) : SessionMessage;

    public record UpdateRendering(RenderingStatus Status) : SessionMessage;
}

class FlatBuffersSession
{
    private static readonly ILogger Logger = Logging.GetLogger("FlatBuffersSession");

    private readonly TcpClient _client;
    private readonly int _clientId;
    private readonly SpecStreamReader _socketSpecReader;
    private readonly SpecStreamWriter _socketSpecWriter;

    private readonly Channel<SessionMessage> _incomingMessages;
    private readonly ChannelWriter<IServerMessage> _rlbotServer;
    private readonly ChannelWriter<IBridgeMessage> _bridge;
    private readonly Dictionary<uint, bool> _gotInput = new();

    /// <summary>Indicates that we received a ConnectionSettings message from the client, and that
    /// we now know its agent id (if any) and whether it is interested in ball prediction, match comms,
    /// and closing between matches.</summary>
    private bool _connectionEstablished;

    private bool _wantsBallPredictions;
    private bool _wantsComms;
    private bool _closeBetweenMatches;

    /// <summary>Indicates that the client have responded with InitComplete after we sent a ControllableTeamInfo.
    /// I.e. it is ready to receive live data. Match runners (with no agent id) never becomes ready.</summary>
    private bool _isReady;

    private bool _stateSettingIsEnabled;
    private bool _renderingIsEnabled;

    private string _agentId = string.Empty;
    private uint _team = Team.Other;
    private List<PlayerIdPair> _playerIdPairs = new();
    private bool _sessionForceClosed;
    private bool _closed;

    public string ClientName =>
        _agentId != ""
            ? $"client {_clientId} (index {string.Join("+", _playerIdPairs.Select(p => p.Index))}, team {_team}, aid {_agentId})"
            : $"client {_clientId} (w/o aid)";

    public FlatBuffersSession(
        TcpClient client,
        int clientId,
        Channel<SessionMessage> incomingMessages,
        ChannelWriter<IServerMessage> rlbotServer,
        ChannelWriter<IBridgeMessage> bridge,
        DebugRendering renderingIsEnabled,
        bool stateSettingIsEnabled
    )
    {
        _client = client;
        _clientId = clientId;
        _incomingMessages = incomingMessages;
        _rlbotServer = rlbotServer;
        _bridge = bridge;
        _renderingIsEnabled = renderingIsEnabled switch
        {
            DebugRendering.OnByDefault => true,
            _ => false,
        };
        _stateSettingIsEnabled = stateSettingIsEnabled;

        NetworkStream stream = _client.GetStream();
        _socketSpecReader = new SpecStreamReader(stream);
        _socketSpecWriter = new SpecStreamWriter(stream);
    }

    private async Task<bool> ParseClientMessage(InterfacePacket msg)
    {
        switch (msg.MessageType)
        {
            case InterfaceMessage.NONE:
                Logger.LogError(
                    $"Received a message with type NONE from {ClientName}. "
                        + "Something has gone very wrong. Disconnecting."
                );
                return false;

            case InterfaceMessage.DisconnectSignal:
                // The client requested that we close the connection
                return false;

            case InterfaceMessage.ConnectionSettings when !_connectionEstablished:
                var readyMsg = msg.MessageAsConnectionSettings();

                _agentId = readyMsg.AgentId ?? "";
                _wantsBallPredictions = readyMsg.WantsBallPredictions;
                _wantsComms = readyMsg.WantsComms;
                _closeBetweenMatches = readyMsg.CloseBetweenMatches;

                if (_agentId != "" && !_closeBetweenMatches)
                {
                    Logger.LogError(
                        $"Detected a client with close_between_matches=False AND a non-empty agent id '{_agentId}'. "
                            + $"These settings are incompatible. Disconnecting."
                    );
                    return false;
                }

                await _rlbotServer.WriteAsync(
                    new IntroDataRequest(_clientId, _incomingMessages.Writer, _agentId)
                );

                _connectionEstablished = true;
                break;

            case InterfaceMessage.SetLoadout when _connectionEstablished:
                if (_isReady && !_stateSettingIsEnabled)
                    break;

                var setLoadout = msg.MessageAsSetLoadout().UnPack();

                if (_isReady)
                {
                    // state setting is enabled,
                    // allow setting the loadout of any bots post-init
                    await _bridge.WriteAsync(
                        new StateSetLoadout(setLoadout.Loadout, setLoadout.Index)
                    );
                }
                else
                {
                    // ensure the provided index is a bot we control,
                    // and map the index to the spawn id
                    PlayerIdPair? maybeIdPair = _playerIdPairs.FirstOrDefault(idPair =>
                        idPair.Index == setLoadout.Index
                    );

                    if (maybeIdPair is { } pair)
                    {
                        await _bridge.WriteAsync(
                            new SetInitLoadout(setLoadout.Loadout, pair.PlayerId)
                        );
                    }
                    else
                    {
                        var owned = string.Join(", ", _playerIdPairs.Select(p => p.Index));
                        Logger.LogWarning(
                            $"Client tried to set loadout of player it does not own "
                                + $"(index(es) owned: {owned},"
                                + $" got: {setLoadout.Index})"
                        );
                    }
                }

                break;

            case InterfaceMessage.InitComplete when _connectionEstablished && !_isReady:
                if (_closeBetweenMatches)
                {
                    await _bridge.WriteAsync(new SessionReady(_clientId));
                }

                Logger.LogDebug("InitComplete from {}", ClientName);
                _isReady = true;
                break;

            case InterfaceMessage.StopCommand:
                var stopCommand = msg.MessageAsStopCommand();
                await _rlbotServer.WriteAsync(new StopMatch(stopCommand.ShutdownServer));
                break;

            case InterfaceMessage.StartCommand:
                var startCommand = msg.MessageAsStartCommand();
                var parser = new ConfigParser();
                if (
                    parser.TryLoadMatchConfig(startCommand.ConfigPath, out var tomlMatchConfig)
                    && ConfigValidator.Validate(tomlMatchConfig)
                )
                {
                    await _rlbotServer.WriteAsync(new StartMatch(tomlMatchConfig));
                }
                break;

            case InterfaceMessage.MatchConfiguration:
                var matchConfig = msg.MessageAsMatchConfiguration().UnPack();
                if (ConfigValidator.Validate(matchConfig))
                {
                    await _rlbotServer.WriteAsync(new StartMatch(matchConfig));
                }
                break;

            case InterfaceMessage.PlayerInput when _connectionEstablished:
                var playerInputMsg = msg.MessageAsPlayerInput().UnPack();

                // ensure the provided index is a bot we control
                if (
                    !_stateSettingIsEnabled
                    && !_playerIdPairs.Any(playerInfo =>
                        playerInfo.Index == playerInputMsg.PlayerIndex
                    )
                )
                {
                    var owned = string.Join(", ", _playerIdPairs.Select(p => p.Index));
                    Logger.LogWarning(
                        $"Client tried to set loadout of player it does not own"
                            + $" (index(es) owned: {owned},"
                            + $" got: {playerInputMsg.PlayerIndex})"
                    );
                    break;
                }

                _gotInput[playerInputMsg.PlayerIndex] = true;
                await _bridge.WriteAsync(new Input(playerInputMsg));
                break;

            case InterfaceMessage.MatchComm:
                var matchComms = msg.MessageAsMatchComm().UnPack();

                if (_agentId != "")
                {
                    // ensure the team is correctly set
                    matchComms.Team = _team;

                    // ensure the provided index is a bot we control,
                    // and map the index to the spawn id
                    PlayerIdPair? playerIdPair = _playerIdPairs.FirstOrDefault(idPair =>
                        idPair.Index == matchComms.Index
                    );

                    if (playerIdPair != null)
                    {
                        await _rlbotServer.WriteAsync(
                            new SendMatchComm(_clientId, matchComms)
                        );

                        await _bridge.WriteAsync(new ShowQuickChat(matchComms));
                    }
                }
                else if (_agentId == "" && _connectionEstablished)
                {
                    // Client is a match manager.
                    // We allow these to send match comms to bots/scripts too, e.g. for briefing.
                    // They will not appear in quick chat.
                    matchComms.Index = 0;
                    matchComms.Team = Team.Other;
                    matchComms.TeamOnly = false;
                    await _rlbotServer.WriteAsync(new SendMatchComm(_clientId, matchComms));
                }

                break;

            case InterfaceMessage.RenderGroup:
                if (!_renderingIsEnabled)
                    break;

                var renderingGroup = msg.MessageAsRenderGroup().UnPack();
                await _bridge.WriteAsync(
                    new AddRenders(_clientId, renderingGroup.Id, renderingGroup.RenderMessages)
                );

                break;

            case InterfaceMessage.RemoveRenderGroup:
                if (!_renderingIsEnabled)
                    break;

                var removeRenderGroup = msg.MessageAsRemoveRenderGroup();
                await _bridge.WriteAsync(new RemoveRenders(_clientId, removeRenderGroup.Id));
                break;

            case InterfaceMessage.DesiredGameState:
                if (!_stateSettingIsEnabled)
                    break;

                var desiredGameState = msg.MessageAsDesiredGameState().UnPack();
                await _bridge.WriteAsync(new SetGameState(desiredGameState));

                break;
        }

        return true;
    }

    private void SendPayloadToClient(CoreMessageUnion payload)
    {
        try
        {
            _socketSpecWriter.Write(payload);
            _socketSpecWriter.Send();
        }
        catch (ObjectDisposedException)
        {
            // we disconnected before the message could be sent
            return;
        }
        catch (IOException)
        {
            // client disconnected before we could send the message
            return;
        }
    }

    private async Task HandleInternalMessages()
    {
        await foreach (SessionMessage message in _incomingMessages.Reader.ReadAllAsync())
            switch (message)
            {
                case SessionMessage.FieldInfo m:
                    SendPayloadToClient(CoreMessageUnion.FromFieldInfo(m.Info));
                    break;
                case SessionMessage.MatchConfig m:
                    SendPayloadToClient(CoreMessageUnion.FromMatchConfiguration(m.Config));
                    break;
                case SessionMessage.PlayerIdPairs m:
                    _team = m.Team;
                    _playerIdPairs = m.IdMaps;

                    List<ControllableInfoT> controllables = _playerIdPairs
                        .Select(playerInfo => new ControllableInfoT()
                        {
                            Index = playerInfo.Index,
                            Identifier = playerInfo.PlayerId,
                        })
                        .ToList();

                    ControllableTeamInfoT playerMappings = new()
                    {
                        Team = m.Team,
                        Controllables = controllables,
                    };

                    SendPayloadToClient(
                        CoreMessageUnion.FromControllableTeamInfo(playerMappings)
                    );

                    Logger.LogDebug("Reserved agents for {}", ClientName);

                    break;
                case SessionMessage.DistributeBallPrediction m
                    when _isReady && _wantsBallPredictions:
                    SendPayloadToClient(CoreMessageUnion.FromBallPrediction(m.BallPrediction));
                    break;
                case SessionMessage.DistributeGameState m when _isReady:
                    SendPayloadToClient(CoreMessageUnion.FromGamePacket(m.GameState));

                    foreach (var (index, gotInput) in _gotInput)
                    {
                        await _bridge.WriteAsync(new AddPerfSample(index, gotInput));
                        _gotInput[index] = false;
                    }

                    break;
                case SessionMessage.RendersAllowed m:
                    _renderingIsEnabled = m.Allowed;
                    break;
                case SessionMessage.StateSettingAllowed m:
                    _stateSettingIsEnabled = m.Allowed;
                    break;
                case SessionMessage.MatchComm m when _wantsComms:
                    // Do not distribute this match comm to our client in certain cases.
                    // Match managers with no agent id receive all messages.
                    if (_agentId != "")
                    {
                        if (!_isReady)
                        {
                            break;
                        }
                        if (m.Message.TeamOnly && m.Message.Team != _team)
                        {
                            break;
                        }
                    }

                    SendPayloadToClient(CoreMessageUnion.FromMatchComm(m.Message));

                    break;
                case SessionMessage.StopMatch m
                    when m.Force || (_connectionEstablished && _closeBetweenMatches):
                    _sessionForceClosed = m.Force;
                    return;
                case SessionMessage.UpdateRendering m
                    when (m.Status.IsBot && (_team == Team.Blue || _team == Team.Orange))
                        || (!m.Status.IsBot && _team == Team.Scripts):

                    foreach (var player in _playerIdPairs)
                    {
                        if (player.Index == m.Status.Index)
                        {
                            _renderingIsEnabled = m.Status.Status;
                            break;
                        }
                    }
                    break;
            }
    }

    private async Task HandleClientMessages()
    {
        await foreach (InterfacePacket message in _socketSpecReader.ReadAllAsync())
        {
            // if the session is closed, ignore any incoming messages
            // this should allow the client to close cleanly
            if (_closed)
                continue;

            bool keepRunning = await ParseClientMessage(message);
            if (keepRunning)
                continue;
            return;
        }
    }

    public void BlockingRun()
    {
        Task incomingMessagesTask = Task.Run(async () =>
        {
            try
            {
                await HandleInternalMessages();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while handling incoming messages: {e}");
            }
        });

        Task clientMessagesTask = Task.Run(async () =>
        {
            try
            {
                await HandleClientMessages();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while handling client messages: {e}");
            }
        });

        Task.WhenAny(incomingMessagesTask, clientMessagesTask).Wait();
    }

    public void Cleanup()
    {
        Logger.LogInformation("Closing session for {}", ClientName);

        _connectionEstablished = false;
        _isReady = false;
        _incomingMessages.Writer.TryComplete();
        _bridge.TryWrite(new UnreserveAgents(_clientId));

        // try to politely close the connection
        try
        {
            SendPayloadToClient(
                CoreMessageUnion.FromDisconnectSignal(new DisconnectSignalT())
            );
        }
        catch (Exception)
        {
            // if an exception was thrown, the client disconnected first
        }
        finally
        {
            // remove this session from the server
            _rlbotServer.TryWrite(new SessionClosed(_clientId));

            // if we're trying to shut down cleanly,
            // let the bot finish sending messages and close the connection itself
            _closed = true;
            if (!_sessionForceClosed)
                _client.Close();
        }
    }
}
