using System.Net.Sockets;
using System.Threading.Channels;
using Google.FlatBuffers;
using MatchManagement;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.Server.FlatbuffersMessage;
using RLBotSecret.Types;

namespace RLBotCS.Server;

internal record SessionMessage
{
    public record DistributeBallPrediction(BallPredictionT BallPrediction) : SessionMessage;

    public record DistributeGameState(RLBotSecret.State.GameState GameState) : SessionMessage;

    public record RendersAllowed(bool Allowed) : SessionMessage;

    public record StateSettingAllowed(bool Allowed) : SessionMessage;

    public record StopMatch : SessionMessage;
}

internal class FlatBuffersSession
{
    private TcpClient _client;
    private int _clientId;
    private SocketSpecStreamReader _socketSpecReader;
    private SocketSpecStreamWriter _socketSpecWriter;

    private ChannelReader<SessionMessage> _incomingMessages;
    private ChannelWriter<IServerMessage> _rlbotServer;
    private ChannelWriter<IBridgeMessage> _bridge;

    private bool _isReady = false;
    private bool _wantsBallPredictions = false;
    private bool _wantsGameMessages = false;
    private bool _wantsComms = false;
    private bool _closeAfterMatch = false;
    private bool _stateSettingIsEnabled;
    private bool _renderingIsEnabled;

    private FlatBufferBuilder _builder = new(1024);

    public FlatBuffersSession(
        TcpClient client,
        int clientId,
        ChannelReader<SessionMessage> incomingMessages,
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
                _wantsGameMessages = readyMsg.WantsGameMessages;
                _wantsComms = readyMsg.WantsComms;
                _closeAfterMatch = readyMsg.CloseAfterMatch;

                Channel<MatchSettingsT> matchSettingsChannel = Channel.CreateUnbounded<MatchSettingsT>();
                Channel<FieldInfoT> fieldInfoChannel = Channel.CreateUnbounded<FieldInfoT>();

                _rlbotServer.TryWrite(new IntroDataRequest(matchSettingsChannel.Writer, fieldInfoChannel.Writer));

                // get then send match settings
                // this is usually return before fieldinfo so we wait on it first
                MatchSettingsT matchSettings = await matchSettingsChannel.Reader.ReadAsync();

                _builder.Clear();
                _builder.Finish(MatchSettings.Pack(_builder, matchSettings).Value);
                TypedPayload matchSettingsMessage = TypedPayload.FromFlatBufferBuilder(
                    DataType.MatchSettings,
                    _builder
                );
                await SendPayloadToClientAsync(matchSettingsMessage);
                Console.WriteLine("Sent match settings to client.");

                // get then send field info
                FieldInfoT fieldInfo = await fieldInfoChannel.Reader.ReadAsync();

                _builder.Clear();
                _builder.Finish(FieldInfo.Pack(_builder, fieldInfo).Value);
                TypedPayload fieldInfoMessage = TypedPayload.FromFlatBufferBuilder(DataType.FieldInfo, _builder);
                await SendPayloadToClientAsync(fieldInfoMessage);
                Console.WriteLine("Sent field info to client.");

                _isReady = true;
                break;

            case DataType.StopCommand:
                Console.WriteLine("Core got stop command from client.");
                var stopCommand = StopCommand.GetRootAsStopCommand(byteBuffer).UnPack();
                _rlbotServer.TryWrite(new StopMatch(stopCommand.ShutdownServer));
                break;

            case DataType.StartCommand:
                Console.WriteLine("Core got start command from client.");
                StartCommandT startCommand = StartCommand.GetRootAsStartCommand(byteBuffer).UnPack();
                MatchSettingsT tomlMatchSettings = ConfigParser.GetMatchSettings(startCommand.ConfigPath);

                _rlbotServer.TryWrite(new StartMatch(tomlMatchSettings));
                break;

            case DataType.MatchSettings:
                var matchSettingsT = MatchSettings.GetRootAsMatchSettings(byteBuffer).UnPack();
                _rlbotServer.TryWrite(new StartMatch(matchSettingsT));
                break;

            case DataType.PlayerInput:
                var playerInputMsg = PlayerInput.GetRootAsPlayerInput(byteBuffer);
                _bridge.TryWrite(new Input(playerInputMsg));
                break;

            case DataType.MatchComms:
                if (!_wantsComms)
                {
                    break;
                }

                var matchComms = MatchComm.GetRootAsMatchComm(byteBuffer).UnPack();
                // todo: send to server to send to other clients

                break;

            case DataType.RenderGroup:
                if (!_renderingIsEnabled)
                {
                    break;
                }

                var renderingGroup = RenderGroup.GetRootAsRenderGroup(byteBuffer).UnPack();
                _bridge.TryWrite(new AddRenders(_clientId, renderingGroup.Id, renderingGroup.RenderMessages));

                break;

            case DataType.RemoveRenderGroup:
                if (!_renderingIsEnabled)
                {
                    break;
                }

                var removeRenderGroup = RemoveRenderGroup.GetRootAsRemoveRenderGroup(byteBuffer).UnPack();
                _bridge.TryWrite(new RemoveRenders(_clientId, removeRenderGroup.Id));
                break;

            case DataType.DesiredGameState:
                if (!_stateSettingIsEnabled)
                {
                    break;
                }

                var desiredGameState = DesiredGameState.GetRootAsDesiredGameState(byteBuffer).UnPack();
                // _gameController.MatchStarter.SetDesiredGameState(desiredGameState);
                // todo

                break;

            default:
                Console.WriteLine("Core got unexpected message type {0} from client.", message.Type);
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
        await foreach (SessionMessage message in _incomingMessages.ReadAllAsync())
            switch (message)
            {
                case SessionMessage.DistributeBallPrediction m when _isReady && _wantsBallPredictions:
                    _builder.Clear();
                    _builder.Finish(BallPrediction.Pack(_builder, m.BallPrediction).Value);

                    await SendPayloadToClientAsync(
                        TypedPayload.FromFlatBufferBuilder(DataType.BallPrediction, _builder)
                    );
                    break;
                case SessionMessage.DistributeGameState m when _isReady:
                    await SendPayloadToClientAsync(m.GameState.ToFlatbuffer(_builder));
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
            TypedPayload msg = new() { Type = DataType.None, Payload = new ArraySegment<byte>([1]), };
            SendPayloadToClientAsync(msg).Wait();
        }
        finally
        {
            // if an exception was thrown, the client disconnected first
            // remove this session from the server
            _rlbotServer.TryWrite(new SessionClosed(_clientId));
            _client.Close();
        }
    }
}
