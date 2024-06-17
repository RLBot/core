using System.Net.Sockets;
using System.Threading.Channels;
using Google.FlatBuffers;
using MatchManagement;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.Server.FlatbuffersMessage;
using RLBotSecret.Types;

namespace RLBotCS.Server
{
    internal record SessionMessage
    {
        public record DistributeGameState(RLBotSecret.State.GameState GameState) : SessionMessage;
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

        private FlatBufferBuilder _builder = new(1024);

        public FlatBuffersSession(
            TcpClient client,
            int clientId,
            ChannelReader<SessionMessage> incomingMessages,
            ChannelWriter<IServerMessage> rlbotServer,
            ChannelWriter<IBridgeMessage> bridge
        )
        {
            _client = client;
            _clientId = clientId;
            _incomingMessages = incomingMessages;
            _rlbotServer = rlbotServer;
            _bridge = bridge;

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

                    _rlbotServer.TryWrite(
                        new IntroDataRequest(matchSettingsChannel.Writer, fieldInfoChannel.Writer)
                    );

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
                    TypedPayload fieldInfoMessage = TypedPayload.FromFlatBufferBuilder(
                        DataType.FieldInfo,
                        _builder
                    );
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
                if (keepRunning) continue;

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
            catch (Exception)
            {
                // client disconnected first
            }

            _rlbotServer.TryWrite(new SessionClosed(_clientId));
            _client.Close();
        }
    }
}