using System.Net.Sockets;
using Google.FlatBuffers;
using MatchManagement;
using rlbot.flat;
using RLBotCS.GameControl;
using RLBotSecret.Conversion;
using RLBotSecret.State;
using RLBotSecret.Types;

namespace RLBotCS.Server
{
    /**
     * Taken from https://codinginfinite.com/multi-threaded-tcp-server-core-example-csharp/
     */
    internal class FlatbufferSession
    {
        private NetworkStream _stream;
        private GameController _gameController;
        private PlayerMapping _playerMapping;
        private SocketSpecStreamWriter _socketSpecWriter;
        private Dictionary<int, List<ushort>> _sessionRenderIds = new();
        private bool _stateSettingIsEnabled = true;
        private bool _renderingIsEnabled = true;
        private bool _startedCommunications = false;
        private bool _requestedServerStop = false;
        private bool _requestedMatchStop = false;
        private bool _matchEnded = false;

        public bool IsReady { get; private set; }

        public bool NeedsIntroData { get; private set; }

        public bool WantsBallPredictions { get; private set; }

        public bool WantsGameMessages { get; private set; }

        public bool WantsComms { get; private set; }

        public bool CloseAfterMatch { get; private set; }

        public FlatbufferSession(
            NetworkStream stream,
            GameController gameController,
            PlayerMapping playerMapping,
            bool startedCommunications
        )
        {
            stream.ReadTimeout = 32;
            this._stream = stream;
            this._gameController = gameController;
            this._playerMapping = playerMapping;
            this._startedCommunications = startedCommunications;
            _socketSpecWriter = new SocketSpecStreamWriter(stream);
        }

        public void RemoveRenders()
        {
            foreach (var renderIds in _sessionRenderIds.Values)
            {
                for (var i = 0; i < renderIds.Count; i++)
                {
                    try
                    {
                        _gameController.RenderingSender.RemoveRenderItem(renderIds[i]);
                    }
                    catch (Exception)
                    {
                        _gameController.RenderingSender.Send();
                        i = 0;
                    }
                }

                _gameController.RenderingSender.Send();
            }

            _sessionRenderIds.Clear();
        }

        public void Close(bool wasDroppedCleanly)
        {
            // If we we're dropped cleanly,
            // it's probably because the connection was terminated
            // So we should skip sending the confirmation shutdown message
            if (wasDroppedCleanly)
            {
                SendShutdownConfirmation();
            }

            _stream.Close();

            RemoveRenders();
        }

        public void RunBlocking()
        {
            foreach (var message in SocketSpecStreamReader.Read(_stream))
            {
                if (_matchEnded)
                {
                    Console.WriteLine("Session is exiting because the match has ended.");
                    return;
                }

                var byteBuffer = new ByteBuffer(message.Payload.Array, message.Payload.Offset);

                switch (message.Type)
                {
                    case DataType.None:
                        // The client requested that we close the connection
                        return;
                    case DataType.ReadyMessage:
                        var readyMsg = ReadyMessage.GetRootAsReadyMessage(byteBuffer);
                        IsReady = true;
                        NeedsIntroData = true;
                        WantsBallPredictions = readyMsg.WantsBallPredictions;
                        WantsGameMessages = readyMsg.WantsGameMessages;
                        WantsComms = readyMsg.WantsComms;
                        CloseAfterMatch = readyMsg.CloseAfterMatch;
                        break;
                    case DataType.StopCommand:
                        _requestedMatchStop = true;
                        var stopCommand = StopCommand.GetRootAsStopCommand(byteBuffer).UnPack();
                        _requestedServerStop = stopCommand.ShutdownServer;
                        Console.WriteLine("Core got stop command from client.");
                        break;
                    case DataType.StartCommand:
                        var startCommand = StartCommand.GetRootAsStartCommand(byteBuffer).UnPack();
                        var tomlMatchSettings = ConfigParser.GetMatchSettings(startCommand.ConfigPath);
                        FlatBufferBuilder builder = new(1500);
                        builder.Finish(rlbot.flat.MatchSettings.Pack(builder, tomlMatchSettings).Value);
                        TypedPayload matchSettingsMessage = TypedPayload.FromFlatBufferBuilder(
                            DataType.MatchSettings,
                            builder
                        );
                        _gameController.MatchStarter.HandleMatchSettings(
                            tomlMatchSettings,
                            matchSettingsMessage,
                            !_startedCommunications
                        );
                        break;
                    case DataType.MatchSettings:
                        var matchSettings = rlbot.flat.MatchSettings.GetRootAsMatchSettings(byteBuffer);
                        _gameController.MatchStarter.HandleMatchSettings(
                            matchSettings.UnPack(),
                            message,
                            !_startedCommunications
                        );
                        break;
                    case DataType.PlayerInput:
                        var playerInputMsg = PlayerInput.GetRootAsPlayerInput(byteBuffer);
                        var carInput = FlatToModel.ToCarInput(playerInputMsg.ControllerState.Value);
                        var actorId = _playerMapping.ActorIdFromPlayerIndex(playerInputMsg.PlayerIndex);
                        if (actorId.HasValue)
                        {
                            var playerInput = new RLBotSecret.Models.Control.PlayerInput()
                            {
                                ActorId = actorId.Value,
                                CarInput = carInput
                            };
                            _gameController.PlayerInputSender.SendPlayerInput(playerInput);
                        }
                        else
                        {
                            Console.WriteLine(
                                "Core got input from unknown player index {0}",
                                playerInputMsg.PlayerIndex
                            );
                        }
                        break;
                    case DataType.MatchComms:
                        break;
                    case DataType.RenderGroup:
                        if (!_renderingIsEnabled)
                        {
                            break;
                        }

                        var renderingGroup = RenderGroup.GetRootAsRenderGroup(byteBuffer).UnPack();

                        // If a group already exists with the same id,
                        // remove the old render items
                        RemoveRenderGroup(renderingGroup.Id);

                        List<ushort> renderIds = new();

                        // Create render requests
                        foreach (var renderMessage in renderingGroup.RenderMessages)
                        {
                            if (RenderItem(renderMessage.Variety) is ushort renderId)
                            {
                                renderIds.Add(renderId);
                            }
                        }

                        // Add the new render items to the tracker
                        _sessionRenderIds[renderingGroup.Id] = renderIds;

                        // Send the render requests
                        _gameController.RenderingSender.Send();

                        break;
                    case DataType.RemoveRenderGroup:
                        var removeRenderGroup = rlbot
                            .flat.RemoveRenderGroup.GetRootAsRemoveRenderGroup(byteBuffer)
                            .UnPack();
                        RemoveRenderGroup(removeRenderGroup.Id);
                        break;
                    case DataType.DesiredGameState:
                        if (!_stateSettingIsEnabled)
                        {
                            break;
                        }

                        var desiredGameState = DesiredGameState.GetRootAsDesiredGameState(byteBuffer).UnPack();
                        _gameController.MatchStarter.SetDesiredGameState(desiredGameState);
                        break;
                    default:
                        Console.WriteLine("Core got unexpected message type {0} from client.", message.Type);
                        break;
                }
            }
        }

        private void SendShutdownConfirmation()
        {
            TypedPayload msg = new() { Type = DataType.None, Payload = new ArraySegment<byte>([1]), };
            SendPayloadToClient(msg);
        }

        private void RemoveRenderGroup(int renderGroupId)
        {
            // If a group already exists with the same id,
            // remove the old render items
            if (_sessionRenderIds.ContainsKey(renderGroupId))
            {
                foreach (var oldRenderId in _sessionRenderIds[renderGroupId])
                {
                    _gameController.RenderingSender.RemoveRenderItem(oldRenderId);
                }

                _sessionRenderIds.Remove(renderGroupId);
                _gameController.RenderingSender.Send();
            }
        }

        private ushort? RenderItem(RenderTypeUnion renderMessage)
        {
            switch (renderMessage.Type)
            {
                case RenderType.Line3D:
                    var lineData = renderMessage.AsLine3D();
                    return _gameController.RenderingSender.AddLine3D(
                        new RLBotSecret.Models.Phys.Vector3()
                        {
                            x = lineData.Start.X,
                            y = lineData.Start.Y,
                            z = lineData.Start.Z,
                        },
                        new RLBotSecret.Models.Phys.Vector3()
                        {
                            x = lineData.End.X,
                            y = lineData.End.Y,
                            z = lineData.End.Z,
                        },
                        System.Drawing.Color.FromArgb(
                            lineData.Color.A,
                            lineData.Color.R,
                            lineData.Color.G,
                            lineData.Color.B
                        )
                    );
                case RenderType.PolyLine3D:
                    var polyLineData = renderMessage.AsPolyLine3D();
                    List<RLBotSecret.Models.Phys.Vector3> points = new();
                    foreach (var point in polyLineData.Points)
                    {
                        points.Add(
                            new RLBotSecret.Models.Phys.Vector3()
                            {
                                x = point.X,
                                y = point.Y,
                                z = point.Z,
                            }
                        );
                    }
                    return _gameController.RenderingSender.AddLine3DSeries(
                        points,
                        System.Drawing.Color.FromArgb(
                            polyLineData.Color.A,
                            polyLineData.Color.R,
                            polyLineData.Color.G,
                            polyLineData.Color.B
                        )
                    );
                case RenderType.String2D:
                    var string2DData = renderMessage.AsString2D();
                    return _gameController.RenderingSender.AddText2D(
                        string2DData.Text,
                        string2DData.X,
                        string2DData.Y,
                        System.Drawing.Color.FromArgb(
                            string2DData.Foreground.A,
                            string2DData.Foreground.R,
                            string2DData.Foreground.G,
                            string2DData.Foreground.B
                        ),
                        System.Drawing.Color.FromArgb(
                            string2DData.Background.A,
                            string2DData.Background.R,
                            string2DData.Background.G,
                            string2DData.Background.B
                        ),
                        (byte)string2DData.HAlign,
                        (byte)string2DData.VAlign,
                        string2DData.Scale
                    );
                case RenderType.String3D:
                    var string3DData = renderMessage.AsString3D();
                    return _gameController.RenderingSender.AddText3D(
                        string3DData.Text,
                        new RLBotSecret.Models.Phys.Vector3()
                        {
                            x = string3DData.Position.X,
                            y = string3DData.Position.Y,
                            z = string3DData.Position.Z,
                        },
                        System.Drawing.Color.FromArgb(
                            string3DData.Foreground.A,
                            string3DData.Foreground.R,
                            string3DData.Foreground.G,
                            string3DData.Foreground.B
                        ),
                        System.Drawing.Color.FromArgb(
                            string3DData.Background.A,
                            string3DData.Background.R,
                            string3DData.Background.G,
                            string3DData.Background.B
                        ),
                        (byte)string3DData.HAlign,
                        (byte)string3DData.VAlign,
                        string3DData.Scale
                    );
                default:
                    return null;
            }
        }

        public void SendIntroData(TypedPayload matchSettings, GameState gameState)
        {
            if (matchSettings.Type != DataType.MatchSettings)
            {
                throw new Exception("Expected match settings, got " + matchSettings.Type);
            }

            _socketSpecWriter.Write(matchSettings);

            List<BoostPadT> boostPads = new();
            foreach (var boostPad in gameState.BoostPads)
            {
                boostPads.Add(
                    new BoostPadT()
                    {
                        Location = new Vector3T()
                        {
                            X = boostPad.SpawnPosition.x,
                            Y = boostPad.SpawnPosition.y,
                            Z = boostPad.SpawnPosition.z,
                        },
                        IsFullBoost = boostPad.IsFullBoost,
                    }
                );
            }

            FieldInfoT fieldInfoT = new() { BoostPads = boostPads, Goals = gameState.Goals, };

            FlatBufferBuilder builder = new(1024);
            var offset = FieldInfo.Pack(builder, fieldInfoT);
            builder.Finish(offset.Value);
            var fieldInfo = TypedPayload.FromFlatBufferBuilder(DataType.FieldInfo, builder);
            _socketSpecWriter.Write(fieldInfo);

            _socketSpecWriter.Send();
            Console.WriteLine("Core sent intro data to client.");
            NeedsIntroData = false;
        }

        public void ToggleStateSetting(bool isEnabled)
        {
            _stateSettingIsEnabled = isEnabled;
        }

        public void ToggleRendering(bool isEnabled)
        {
            _stateSettingIsEnabled = isEnabled;
        }

        internal void SendPayloadToClient(TypedPayload payload)
        {
            _socketSpecWriter.Write(payload);
            _socketSpecWriter.Send();
        }

        internal void SetStartCommunications(bool startedCommunications)
        {
            this._startedCommunications = startedCommunications;
        }

        internal bool HasRequestedServerStop()
        {
            if (_requestedServerStop)
            {
                _requestedServerStop = false;
                return true;
            }

            return false;
        }

        internal bool HasRequestedMatchStop()
        {
            if (_requestedMatchStop)
            {
                _requestedMatchStop = false;
                return true;
            }

            return false;
        }

        internal void SetMatchEnded()
        {
            Console.WriteLine("Setting match ended in session.");
            _matchEnded = true;
        }
    }
}
