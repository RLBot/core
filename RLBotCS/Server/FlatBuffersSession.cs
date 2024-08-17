using System.Net.Sockets;
using System.Threading.Channels;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.ManagerTools;
using RLBotCS.Server.FlatbuffersMessage;
using RLBotCS.Types;

namespace RLBotCS.Server;

internal record SessionMessage
{
    public record MatchSettings(MatchSettingsT Settings) : SessionMessage;

    public record FieldInfo(FieldInfoT Info) : SessionMessage;

    public record DistributeBallPrediction(BallPredictionT BallPrediction) : SessionMessage;

    public record DistributeGameState(GameTickPacketT GameState) : SessionMessage;

    public record RendersAllowed(bool Allowed) : SessionMessage;

    public record StateSettingAllowed(bool Allowed) : SessionMessage;

    public record MatchComm(MatchCommT Message) : SessionMessage;

    public record StopMatch(bool Force) : SessionMessage;
}

internal class FlatBuffersSession
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

    private bool _connectionEstablished;
    private bool _wantsBallPredictions;
    private bool _wantsComms;
    private bool _closeAfterMatch;
    private bool _isReady;
    private bool _stateSettingIsEnabled;
    private bool _renderingIsEnabled;

    private int _spawnId;
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

                _wantsBallPredictions = readyMsg.WantsBallPredictions;
                _wantsComms = readyMsg.WantsComms;
                _closeAfterMatch = readyMsg.CloseAfterMatch;

                await _rlbotServer.WriteAsync(new IntroDataRequest(_incomingMessages.Writer));

                _connectionEstablished = true;
                break;

            case DataType.SetLoadout when !_isReady || _stateSettingIsEnabled:
                var setLoadout = SetLoadout.GetRootAsSetLoadout(byteBuffer).UnPack();

                await _rlbotServer.WriteAsync(
                    new SpawnLoadout(setLoadout.Loadout, setLoadout.SpawnId)
                );
                break;

            case DataType.InitComplete when _connectionEstablished && !_isReady:
                var initComplete = InitComplete.GetRootAsInitComplete(byteBuffer);

                _spawnId = initComplete.SpawnId;

                await _rlbotServer.WriteAsync(
                    new SessionReady(_closeAfterMatch, _clientId, _spawnId)
                );
                _isReady = true;
                break;

            case DataType.StopCommand:
                var stopCommand = StopCommand.GetRootAsStopCommand(byteBuffer);
                await _rlbotServer.WriteAsync(new StopMatch(stopCommand.ShutdownServer));
                break;

            case DataType.StartCommand:
                var startCommand = StartCommand.GetRootAsStartCommand(byteBuffer);
                MatchSettingsT tomlMatchSettings = ConfigParser.GetMatchSettings(
                    startCommand.ConfigPath
                );

                await _rlbotServer.WriteAsync(new StartMatch(tomlMatchSettings));
                break;

            case DataType.MatchSettings:
                var matchSettingsT = MatchSettings.GetRootAsMatchSettings(byteBuffer).UnPack();
                await _rlbotServer.WriteAsync(new StartMatch(matchSettingsT));
                break;

            case DataType.PlayerInput:
                var playerInputMsg = PlayerInput.GetRootAsPlayerInput(byteBuffer).UnPack();
                _gotInput[playerInputMsg.PlayerIndex] = true;

                await _bridge.WriteAsync(new Input(playerInputMsg));
                break;

            case DataType.MatchComms when _wantsComms:
                var matchComms = MatchComm.GetRootAsMatchComm(byteBuffer).UnPack();
                await _rlbotServer.WriteAsync(
                    new SendMatchComm(_clientId, _spawnId, matchComms)
                );
                await _bridge.WriteAsync(new ShowQuickChat(matchComms));

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

            case DataType.GameTickPacket:
                break;
            case DataType.FieldInfo:
                break;
            case DataType.BallPrediction:
                break;
            default:
                Logger.LogError(
                    $"Core got unexpected message type {message.Type} from client."
                );
                break;
        }

        return true;
    }

    private async Task SendPayloadToClientAsync(TypedPayload payload)
    {
        try
        {
            await _socketSpecWriter.WriteAsync(payload);
            await _socketSpecWriter.SendAsync();
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

                    await SendPayloadToClientAsync(
                        TypedPayload.FromFlatBufferBuilder(DataType.FieldInfo, _messageBuilder)
                    );
                    break;
                case SessionMessage.MatchSettings m:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(
                        MatchSettings.Pack(_messageBuilder, m.Settings).Value
                    );

                    await SendPayloadToClientAsync(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.MatchSettings,
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

                    await SendPayloadToClientAsync(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.BallPrediction,
                            _messageBuilder
                        )
                    );
                    break;
                case SessionMessage.DistributeGameState m when _isReady:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(
                        GameTickPacket.Pack(_messageBuilder, m.GameState).Value
                    );

                    await SendPayloadToClientAsync(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.GameTickPacket,
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

                    await SendPayloadToClientAsync(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.MatchComms,
                            _messageBuilder
                        )
                    );

                    break;
                case SessionMessage.StopMatch m
                    when m.Force || (_connectionEstablished && _closeAfterMatch):
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
        Logger.LogInformation($"Closing session {_clientId}.");

        _connectionEstablished = false;
        _isReady = false;
        _incomingMessages.Writer.TryComplete();

        // try to politely close the connection
        try
        {
            TypedPayload msg =
                new() { Type = DataType.None, Payload = new ArraySegment<byte>([1]), };
            SendPayloadToClientAsync(msg).Wait();
        }
        catch (Exception) { }
        finally
        {
            // if an exception was thrown, the client disconnected first
            // remove this session from the server
            _rlbotServer.TryWrite(new SessionClosed(_clientId));

            // if we're trying to shutdown cleanly,
            // let the bot finish sending messages and close the connection itself
            _closed = true;
            if (!_sessionForceClosed)
                _client.Close();
        }
    }
}
