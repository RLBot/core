using System.Net.Sockets;
using System.Threading.Channels;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.Conversion;
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

    public record MatchComm(MatchCommT matchComm) : SessionMessage;

    public record StopMatch : SessionMessage;
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

    private bool _connectionEstablished;
    private bool _wantsBallPredictions;
    private bool _wantsComms;
    private bool _closeAfterMatch;
    private bool _stateSettingIsEnabled;
    private bool _renderingIsEnabled;

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

            case DataType.ReadyMessage when !_connectionEstablished:
                var readyMsg = ReadyMessage.GetRootAsReadyMessage(byteBuffer);

                _wantsBallPredictions = readyMsg.WantsBallPredictions;
                _wantsComms = readyMsg.WantsComms;
                _closeAfterMatch = readyMsg.CloseAfterMatch;

                await _rlbotServer.WriteAsync(new IntroDataRequest(_incomingMessages.Writer));

                if (_closeAfterMatch)
                    await _rlbotServer.WriteAsync(new SessionReady());

                _connectionEstablished = true;
                break;

            case DataType.StopCommand:
                var stopCommand = StopCommand.GetRootAsStopCommand(byteBuffer).UnPack();
                await _rlbotServer.WriteAsync(new StopMatch(stopCommand.ShutdownServer));
                break;

            case DataType.StartCommand:
                StartCommandT startCommand = StartCommand
                    .GetRootAsStartCommand(byteBuffer)
                    .UnPack();
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
                await _bridge.WriteAsync(new Input(playerInputMsg));
                break;

            case DataType.MatchComms:
                if (!_wantsComms)
                    break;

                var matchComms = MatchComm.GetRootAsMatchComm(byteBuffer).UnPack();
                await _rlbotServer.WriteAsync(new SendMatchComm(_clientId, matchComms));
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

                var removeRenderGroup = RemoveRenderGroup
                    .GetRootAsRemoveRenderGroup(byteBuffer)
                    .UnPack();
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
                    when _connectionEstablished && _wantsBallPredictions:
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
                case SessionMessage.DistributeGameState m when _connectionEstablished:
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
                    break;
                case SessionMessage.RendersAllowed m:
                    _renderingIsEnabled = m.Allowed;
                    break;
                case SessionMessage.StateSettingAllowed m:
                    _stateSettingIsEnabled = m.Allowed;
                    break;
                case SessionMessage.MatchComm m when _connectionEstablished && _wantsComms:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(MatchComm.Pack(_messageBuilder, m.matchComm).Value);

                    await SendPayloadToClientAsync(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.MatchComms,
                            _messageBuilder
                        )
                    );

                    break;
                case SessionMessage.StopMatch when _connectionEstablished && _closeAfterMatch:
                    return;
            }
    }

    private async Task HandleClientMessages()
    {
        await foreach (TypedPayload message in _socketSpecReader.ReadAllAsync())
        {
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
            _client.Close();
        }
    }
}
