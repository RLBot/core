using System.Net.Sockets;
using System.Threading.Channels;
using Google.FlatBuffers;
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

    public record DistributeGameState(Bridge.State.GameState GameState) : SessionMessage;

    public record RendersAllowed(bool Allowed) : SessionMessage;

    public record StateSettingAllowed(bool Allowed) : SessionMessage;

    public record MatchComm(MatchCommT matchComm) : SessionMessage;

    public record StopMatch : SessionMessage;
}

internal class FlatBuffersSession
{
    private readonly TcpClient _client;
    private readonly int _clientId;
    private readonly SocketSpecStreamReader _socketSpecReader;
    private readonly SocketSpecStreamWriter _socketSpecWriter;

    private readonly Channel<SessionMessage> _incomingMessages;
    private readonly ChannelWriter<IServerMessage> _rlbotServer;
    private readonly ChannelWriter<IBridgeMessage> _bridge;

    private bool _isReady;
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

            case DataType.ReadyMessage:
                var readyMsg = ReadyMessage.GetRootAsReadyMessage(byteBuffer);

                _wantsBallPredictions = readyMsg.WantsBallPredictions;
                _wantsComms = readyMsg.WantsComms;
                _closeAfterMatch = readyMsg.CloseAfterMatch;

                await _rlbotServer.WriteAsync(new IntroDataRequest(_incomingMessages.Writer));

                _isReady = true;
                break;

            case DataType.StopCommand:
                Console.WriteLine("Core got stop command from client.");
                var stopCommand = StopCommand.GetRootAsStopCommand(byteBuffer).UnPack();
                await _rlbotServer.WriteAsync(new StopMatch(stopCommand.ShutdownServer));
                break;

            case DataType.StartCommand:
                Console.WriteLine("Core got start command from client.");
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
                Console.WriteLine(
                    "Core got unexpected message type {0} from client.",
                    message.Type
                );
                break;
        }

        return true;
    }

    private async Task SendPayloadToClientAsync(TypedPayload payload)
    {
        await _socketSpecWriter.WriteAsync(payload);
        await _socketSpecWriter.SendAsync();
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
                    await SendPayloadToClientAsync(m.GameState.ToFlatBuffers(_messageBuilder));
                    break;
                case SessionMessage.RendersAllowed m:
                    _renderingIsEnabled = m.Allowed;
                    break;
                case SessionMessage.StateSettingAllowed m:
                    _stateSettingIsEnabled = m.Allowed;
                    break;
                case SessionMessage.MatchComm m when _isReady && _wantsComms:
                    _messageBuilder.Clear();
                    _messageBuilder.Finish(MatchComm.Pack(_messageBuilder, m.matchComm).Value);

                    await SendPayloadToClientAsync(
                        TypedPayload.FromFlatBufferBuilder(
                            DataType.MatchComms,
                            _messageBuilder
                        )
                    );

                    break;
                case SessionMessage.StopMatch when _isReady && _closeAfterMatch:
                    Console.WriteLine("Core got stop match message from server.");
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

            Console.WriteLine("Core got close message from client.");
            return;
        }
    }

    public void BlockingRun()
    {
        Task incomingMessagesTask = HandleIncomingMessages();
        Task clientMessagesTask = HandleClientMessages();

        Task.WhenAny(incomingMessagesTask, clientMessagesTask).Wait();
    }

    public void Cleanup()
    {
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
