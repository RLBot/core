using System.Net.Sockets;
using System.Threading.Channels;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.ManagerTools;
using RLBotCS.Server.BridgeMessage;
using RLBotCS.Server.ServerMessage;
using RLBotCS.Types;

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
}

class FlatBuffersSession
{
    private static readonly ILogger Logger = Logging.GetLogger("FlatBuffersSession");

    private readonly TcpClient _client;
    private readonly int _clientId;
    private readonly SocketSpecStreamReader _socketSpecReader;
    private readonly SocketSpecStreamWriter _socketSpecWriter;

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
    private uint _team;
    private List<PlayerIdPair> _playerIdPairs = new();
    private bool _sessionForceClosed;
    private bool _closed;

    private readonly FlatBufferBuilder _messageBuilder = new(1 << 10);

    public FlatBuffersSession(
        TcpClient client,
        int clientId,
        Channel<SessionMessage> incomingMessages,
        ChannelWriter<IServerMessage> rlbotServer,
        ChannelWriter<IBridgeMessage> bridge,
        bool renderingIsEnabled,
        bool stateSettingIsEnabled
    )
    {
        _client = client;
        _clientId = clientId;
        _incomingMessages = incomingMessages;
        _rlbotServer = rlbotServer;
        _bridge = bridge;
        _renderingIsEnabled = renderingIsEnabled;
        _stateSettingIsEnabled = stateSettingIsEnabled;

        NetworkStream stream = _client.GetStream();
        _socketSpecReader = new SocketSpecStreamReader(stream);
        _socketSpecWriter = new SocketSpecStreamWriter(stream);
    }

    private async Task<bool> ParseClientMessage(TypedPayload message)
    {
        ByteBuffer byteBuffer = new(message.Payload.Array, message.Payload.Offset);

        switch (message.Type)
        {
            case DataType.None:
                // The client requested that we close the connection
                return false;

            case DataType.ConnectionSettings when !_connectionEstablished:
                var readyMsg = ConnectionSettings.GetRootAsConnectionSettings(byteBuffer);

                _agentId = readyMsg.AgentId;
                _wantsBallPredictions = readyMsg.WantsBallPredictions;
                _wantsComms = readyMsg.WantsComms;
                _closeBetweenMatches = readyMsg.CloseBetweenMatches;

                if (_agentId != "" && !_closeBetweenMatches)
                {
                    Logger.LogError(
                        $"Detected a client with close_between_matches=False AND a non-empty agent id '{_agentId}'. " +
                        $"These settings are incompatible. Disconnecting."
                    );
                    return false;
                }

                await _rlbotServer.WriteAsync(
                    new IntroDataRequest(_incomingMessages.Writer, _agentId)
                );

                _connectionEstablished = true;
                break;

            case DataType.SetLoadout when _connectionEstablished:
                if (_isReady && !_stateSettingIsEnabled)
                    break;

                var setLoadout = SetLoadout.GetRootAsSetLoadout(byteBuffer).UnPack();

                // ensure the provided index is a bot we control,
                // and map the index to the spawn id
                PlayerIdPair? maybeIdPair = _playerIdPairs.FirstOrDefault(idPair =>
                    idPair.Index == setLoadout.Index
                );

                if (maybeIdPair is { } pair)
                {
                    await _rlbotServer.WriteAsync(
                        new SpawnLoadout(setLoadout.Loadout, pair.SpawnId)
                    );
                }
                else
                {
                    var owned = string.Join(", ", _playerIdPairs.Select(p => p.Index));
                    Logger.LogWarning(
                        $"Client sent loadout unowned player"
                            + $"(index(es) owned: {owned},"
                            + $"index got: {setLoadout.Index})"
                    );
                }

                break;

            case DataType.InitComplete when _connectionEstablished && !_isReady:
                // use the first spawn id we have
                PlayerIdPair? idPair = _playerIdPairs.FirstOrDefault();
                await _rlbotServer.WriteAsync(
                    new SessionReady(_closeBetweenMatches, _clientId, idPair?.SpawnId ?? 0)
                );

                _isReady = true;
                break;

            case DataType.StopCommand:
                var stopCommand = StopCommand.GetRootAsStopCommand(byteBuffer);
                await _rlbotServer.WriteAsync(new StopMatch(stopCommand.ShutdownServer));
                break;

            case DataType.StartCommand:
                var startCommand = StartCommand.GetRootAsStartCommand(byteBuffer);
                var parser = new ConfigParser();
                if (
                    parser.TryLoadMatchConfig(startCommand.ConfigPath, out var tomlMatchConfig)
                    && ConfigValidator.Validate(tomlMatchConfig)
                )
                {
                    await _rlbotServer.WriteAsync(new StartMatch(tomlMatchConfig));
                }
                break;

            case DataType.MatchConfig:
                var matchConfig = MatchConfiguration
                    .GetRootAsMatchConfiguration(byteBuffer)
                    .UnPack();
                if (ConfigValidator.Validate(matchConfig))
                {
                    await _rlbotServer.WriteAsync(new StartMatch(matchConfig));
                }
                break;

            case DataType.PlayerInput when _connectionEstablished:
                var playerInputMsg = PlayerInput.GetRootAsPlayerInput(byteBuffer).UnPack();

                // ensure the provided index is a bot we control
                if (
                    !_playerIdPairs.Any(playerInfo =>
                        playerInfo.Index == playerInputMsg.PlayerIndex
                    )
                )
                {
                    var owned = string.Join(", ", _playerIdPairs.Select(p => p.Index));
                    Logger.LogWarning(
                        $"Client sent player input unowned player"
                            + $"(index(es) owned: {owned},"
                            + $"index got: {playerInputMsg.PlayerIndex})"
                    );
                    break;
                }

                _gotInput[playerInputMsg.PlayerIndex] = true;
                await _bridge.WriteAsync(new Input(playerInputMsg));
                break;

            case DataType.MatchComms when _wantsComms:
                var matchComms = MatchComm.GetRootAsMatchComm(byteBuffer).UnPack();

                // ensure the team is correctly set
                matchComms.Team = _team;

                // ensure the provided index is a bot we control,
                // and map the index to the spawn id
                PlayerIdPair? playerIdPair = _playerIdPairs.FirstOrDefault(idPair =>
                    idPair.Index == matchComms.Index
                );

                if (playerIdPair is PlayerIdPair pInfo && pInfo.SpawnId is int pSpawnId)
                {
                    await _rlbotServer.WriteAsync(
                        new SendMatchComm(_clientId, pSpawnId, matchComms)
                    );

                    await _bridge.WriteAsync(new ShowQuickChat(matchComms));
                }

                break;

            case DataType.RenderGroup:
                if (!_renderingIsEnabled)
                    break;

                var renderingGroup = RenderGroup.GetRootAsRenderGroup(byteBuffer).UnPack();
                await _bridge.WriteAsync(
                    new AddRenders(_clientId, renderingGroup.Id, renderingGroup.RenderMessages)
                );

                break;

            case DataType.RemoveRenderGroup:
                if (!_renderingIsEnabled)
                    break;

                var removeRenderGroup = RemoveRenderGroup.GetRootAsRemoveRenderGroup(
                    byteBuffer
                );
                await _bridge.WriteAsync(new RemoveRenders(_clientId, removeRenderGroup.Id));
                break;

            case DataType.DesiredGameState:
                if (!_stateSettingIsEnabled)
                    break;

                var desiredGameState = DesiredGameState
                    .GetRootAsDesiredGameState(byteBuffer)
                    .UnPack();
                await _bridge.WriteAsync(new SetGameState(desiredGameState));

                break;

            case DataType.GamePacket:
            case DataType.FieldInfo:
            case DataType.BallPrediction:
            default:
                Logger.LogError(
                    $"Core got unexpected message type {message.Type} from client. "
                        + $"Got ConnectionSettings: {_connectionEstablished}."
                );
                break;
        }

        return true;
    }

    private void SendPayloadToClient(TypedPayload payload)
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

    private async Task HandleIncomingMessages()
    {
        await foreach (SessionMessage message in _incomingMessages.Reader.ReadAllAsync())
            switch (message)
            {
                case SessionMessage.FieldInfo m:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(FieldInfo.Pack(_messageBuilder, m.Info).Value);

                    SendPayloadToClient(
                        TypedPayload.FromFlatBufferBuilder(DataType.FieldInfo, _messageBuilder)
                    );
                    break;
                case SessionMessage.MatchConfig m:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(
                        MatchConfiguration.Pack(_messageBuilder, m.Config).Value
                    );

                    SendPayloadToClient(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.MatchConfig,
                            _messageBuilder
                        )
                    );
                    break;
                case SessionMessage.PlayerIdPairs m:
                    _team = m.Team;
                    _playerIdPairs = m.IdMaps;

                    List<ControllableInfoT> controllables = new(
                        _playerIdPairs.Select(playerInfo => new ControllableInfoT()
                        {
                            Index = playerInfo.Index,
                            SpawnId = playerInfo.SpawnId,
                        })
                    );

                    ControllableTeamInfoT playerMappings = new()
                    {
                        Team = m.Team,
                        Controllables = controllables,
                    };

                    _messageBuilder.Clear();
                    _messageBuilder.Finish(
                        ControllableTeamInfo.Pack(_messageBuilder, playerMappings).Value
                    );

                    SendPayloadToClient(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.ControllableTeamInfo,
                            _messageBuilder
                        )
                    );

                    break;
                case SessionMessage.DistributeBallPrediction m
                    when _isReady && _wantsBallPredictions:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(
                        BallPrediction.Pack(_messageBuilder, m.BallPrediction).Value
                    );

                    SendPayloadToClient(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.BallPrediction,
                            _messageBuilder
                        )
                    );
                    break;
                case SessionMessage.DistributeGameState m when _isReady:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(
                        GamePacket.Pack(_messageBuilder, m.GameState).Value
                    );

                    SendPayloadToClient(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.GamePacket,
                            _messageBuilder
                        )
                    );

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
                case SessionMessage.MatchComm m when _isReady && _wantsComms:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(MatchComm.Pack(_messageBuilder, m.Message).Value);

                    SendPayloadToClient(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.MatchComms,
                            _messageBuilder
                        )
                    );

                    break;
                case SessionMessage.StopMatch m
                    when m.Force || (_connectionEstablished && _closeBetweenMatches):
                    _sessionForceClosed = m.Force;
                    return;
            }
    }

    private async Task HandleClientMessages()
    {
        await foreach (TypedPayload message in _socketSpecReader.ReadAllAsync())
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
                await HandleIncomingMessages();
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
        var clientName =
            _agentId != ""
                ? $"{_agentId} (index {string.Join(",", _playerIdPairs.Select(p => p.Index))})"
                : "Client w/o agent id";
        Logger.LogInformation($"Closing session {_clientId} :: {clientName}");

        _connectionEstablished = false;
        _isReady = false;
        _incomingMessages.Writer.TryComplete();
        _bridge.TryWrite(new UnreservePlayers(_team, _playerIdPairs));

        // try to politely close the connection
        try
        {
            TypedPayload msg = new()
            {
                Type = DataType.None,
                Payload = new ArraySegment<byte>([1]),
            };
            SendPayloadToClient(msg);
        }
        catch (Exception) { }
        finally
        {
            // if an exception was thrown, the client disconnected first
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
