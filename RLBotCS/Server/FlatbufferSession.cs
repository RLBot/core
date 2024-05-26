using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Channels;
using Google.FlatBuffers;
using MatchManagement;
using rlbot.flat;
using RLBotSecret.Types;

namespace RLBotCS.Server
{
    internal enum SessionMessageType
    {
        DistributeGameState,
        StopMatch,
    }

    internal class SessionMessage
    {
        private SessionMessageType _type;
        private TypedPayload _gameState;

        public static SessionMessage DistributeGameState(TypedPayload gameState)
        {
            return new SessionMessage { _type = SessionMessageType.DistributeGameState, _gameState = gameState };
        }

        public static SessionMessage StopMatch()
        {
            return new SessionMessage { _type = SessionMessageType.StopMatch };
        }

        public SessionMessageType Type()
        {
            return _type;
        }

        public TypedPayload GetGameState()
        {
            return _gameState;
        }
    }

    internal class FlatbufferSession
    {
        private TcpClient _client;
        private int _clientId;
        private SocketSpecStreamReader _socketSpecReader;
        private SocketSpecStreamWriter _socketSpecWriter;

        private ChannelReader<SessionMessage> _incomingMessages;
        private ChannelWriter<ServerMessage> _rlbotServer;

        private bool _isReady = false;
        private bool _needsIntroData = false;

        private bool _wantsBallPredictions = false;
        private bool _wantsGameMessages = false;
        private bool _wantsComms = false;
        private bool _closeAfterMatch = false;

        public FlatbufferSession(
            TcpClient client,
            int clientId,
            ChannelReader<SessionMessage> incomingMessages,
            ChannelWriter<ServerMessage> rlbotServer
        )
        {
            _client = client;
            _clientId = clientId;
            _socketSpecReader = new SocketSpecStreamReader(_client);
            _socketSpecWriter = new SocketSpecStreamWriter(_client.GetStream());
            _incomingMessages = incomingMessages;
            _rlbotServer = rlbotServer;
        }

        private bool ParseClientMessage(TypedPayload message)
        {
            var byteBuffer = new ByteBuffer(message.Payload.Array, message.Payload.Offset);

            switch (message.Type)
            {
                case DataType.None:
                    // The client requested that we close the connection
                    return false;

                case DataType.ReadyMessage:
                    var readyMsg = ReadyMessage.GetRootAsReadyMessage(byteBuffer);

                    _isReady = true;
                    _needsIntroData = true;
                    _wantsBallPredictions = readyMsg.WantsBallPredictions;
                    _wantsGameMessages = readyMsg.WantsGameMessages;
                    _wantsComms = readyMsg.WantsComms;
                    _closeAfterMatch = readyMsg.CloseAfterMatch;

                    break;

                case DataType.StopCommand:
                    Console.WriteLine("Core got stop command from client.");
                    var stopCommand = StopCommand.GetRootAsStopCommand(byteBuffer).UnPack();
                    _rlbotServer.TryWrite(ServerMessage.StopMatch(stopCommand.ShutdownServer));
                    break;

                case DataType.StartCommand:
                    Console.WriteLine("Core got start command from client.");
                    var startCommand = StartCommand.GetRootAsStartCommand(byteBuffer).UnPack();
                    var tomlMatchSettings = ConfigParser.GetMatchSettings(startCommand.ConfigPath);

                    FlatBufferBuilder builder = new(1500);
                    builder.Finish(MatchSettings.Pack(builder, tomlMatchSettings).Value);
                    TypedPayload matchSettingsMessage = TypedPayload.FromFlatBufferBuilder(
                        DataType.MatchSettings,
                        builder
                    );

                    _rlbotServer.TryWrite(ServerMessage.StartMatch(matchSettingsMessage, tomlMatchSettings));
                    break;

                case DataType.MatchSettings:
                    var matchSettings = MatchSettings.GetRootAsMatchSettings(byteBuffer).UnPack();
                    _rlbotServer.TryWrite(ServerMessage.StartMatch(message, matchSettings));
                    break;

                // case DataType.PlayerInput:
                //     var playerInputMsg = PlayerInput.GetRootAsPlayerInput(byteBuffer);
                //     var carInput = FlatToModel.ToCarInput(playerInputMsg.ControllerState.Value);
                //     var actorId = _playerMapping.ActorIdFromPlayerIndex(playerInputMsg.PlayerIndex);
                //     if (actorId.HasValue)
                //     {
                //         var playerInput = new RLBotSecret.Models.Control.PlayerInput()
                //         {
                //             ActorId = actorId.Value,
                //             CarInput = carInput
                //         };
                //         _gameController.PlayerInputSender.SendPlayerInput(playerInput);
                //     }
                //     else
                //     {
                //         Console.WriteLine(
                //             "Core got input from unknown player index {0}",
                //             playerInputMsg.PlayerIndex
                //         );
                //     }
                //     break;

                case DataType.MatchComms:
                    break;

                // case DataType.RenderGroup:
                //     if (!_renderingIsEnabled)
                //     {
                //         break;
                //     }

                //     var renderingGroup = RenderGroup.GetRootAsRenderGroup(byteBuffer).UnPack();

                //     // If a group already exists with the same id,
                //     // remove the old render items
                //     RemoveRenderGroup(renderingGroup.Id);

                //     List<ushort> renderIds = new();

                //     // Create render requests
                //     foreach (var renderMessage in renderingGroup.RenderMessages)
                //     {
                //         if (RenderItem(renderMessage.Variety) is ushort renderId)
                //         {
                //             renderIds.Add(renderId);
                //         }
                //     }

                //     // Add the new render items to the tracker
                //     _sessionRenderIds[renderingGroup.Id] = renderIds;

                //     // Send the render requests
                //     _gameController.RenderingSender.Send();

                //     break;

                // case DataType.RemoveRenderGroup:
                //     var removeRenderGroup = rlbot
                //         .flat.RemoveRenderGroup.GetRootAsRemoveRenderGroup(byteBuffer)
                //         .UnPack();
                //     RemoveRenderGroup(removeRenderGroup.Id);
                //     break;

                // case DataType.DesiredGameState:
                //     if (!_stateSettingIsEnabled)
                //     {
                //         break;
                //     }

                //     var desiredGameState = DesiredGameState.GetRootAsDesiredGameState(byteBuffer).UnPack();
                //     _gameController.MatchStarter.SetDesiredGameState(desiredGameState);
                //     break;
                default:
                    Console.WriteLine("Core got unexpected message type {0} from client.", message.Type);
                    break;
            }

            return true;
        }

        private void SendPayloadToClient(TypedPayload payload)
        {
            _socketSpecWriter.Write(payload);
            _socketSpecWriter.Send();
        }

        private void SendShutdownConfirmation()
        {
            TypedPayload msg = new() { Type = DataType.None, Payload = new ArraySegment<byte>([1]), };
            SendPayloadToClient(msg);
        }

        private bool IsConnected()
        {
            var state = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .FirstOrDefault(x => x.RemoteEndPoint.Equals(_client.Client.RemoteEndPoint));
            return state != null && state.State == TcpState.Established;
        }

        public void BlockingRun()
        {
            SpinWait spinWait = new SpinWait();

            while (true)
            {
                bool handledSomething = false;

                // check for messages from the client
                while (_socketSpecReader.TryRead(out TypedPayload message))
                {
                    handledSomething = true;
                    if (!ParseClientMessage(message))
                    {
                        return;
                    }
                }

                if (!IsConnected())
                {
                    // ensure that upon disconnect, we handled messages first
                    return;
                }

                if (_incomingMessages.Completion.IsCompleted)
                {
                    SendShutdownConfirmation();
                    return;
                }

                // check for messages from the server
                while (_incomingMessages.TryRead(out SessionMessage message))
                {
                    handledSomething = true;

                    switch (message.Type())
                    {
                        case SessionMessageType.DistributeGameState:
                            SendPayloadToClient(message.GetGameState());
                            break;
                        case SessionMessageType.StopMatch:
                            if (_closeAfterMatch)
                            {
                                SendShutdownConfirmation();
                                return;
                            }

                            break;
                    }
                }

                if (!handledSomething)
                {
                    spinWait.SpinOnce();
                }
            }
        }

        public void Cleanup()
        {
            _client.Close();
            _rlbotServer.TryWrite(ServerMessage.SessionClosed(_clientId));
        }
    }
}
