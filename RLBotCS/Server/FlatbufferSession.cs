using System.Net.Sockets;
using Google.FlatBuffers;
using MatchManagement;
using rlbot.flat;
using RLBotCS.GameControl;
using RLBotSecret.Conversion;
using RLBotSecret.GameState;
using RLBotSecret.Types;

namespace RLBotCS.Server
{
    /**
     * Taken from https://codinginfinite.com/multi-threaded-tcp-server-core-example-csharp/
     */
    internal class FlatbufferSession
    {
        private NetworkStream stream;
        private GameController gameController;
        private PlayerMapping playerMapping;
        private SocketSpecStreamWriter socketSpecWriter;
        private Dictionary<int, List<ushort>> sessionRenderIds = new();
        private bool stateSettingIsEnabled = true;
        private bool renderingIsEnabled = true;
        private bool startedCommunications = false;
        private bool requestedServerStop = false;
        private bool requestedMatchStop = false;
        private bool matchEnded = false;

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
            this.stream = stream;
            this.gameController = gameController;
            this.playerMapping = playerMapping;
            this.startedCommunications = startedCommunications;
            socketSpecWriter = new SocketSpecStreamWriter(stream);
        }

        public void RemoveRenders()
        {
            foreach (var renderIds in sessionRenderIds.Values)
            {
                for (var i = 0; i < renderIds.Count; i++)
                {
                    try
                    {
                        gameController.renderingSender.RemoveRenderItem(renderIds[i]);
                    }
                    catch (Exception)
                    {
                        gameController.renderingSender.Send();
                        i = 0;
                    }
                }

                gameController.renderingSender.Send();
            }

            sessionRenderIds.Clear();
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

            stream.Close();

            RemoveRenders();
        }

        public void RunBlocking()
        {
            foreach (var message in SocketSpecStreamReader.Read(stream))
            {
                if (matchEnded)
                {
                    Console.WriteLine("Session is exiting because the match has ended.");
                    return;
                }

                var byteBuffer = new ByteBuffer(message.payload.Array, message.payload.Offset);

                switch (message.type)
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
                        requestedMatchStop = true;
                        var stopCommand = StopCommand.GetRootAsStopCommand(byteBuffer).UnPack();
                        requestedServerStop = stopCommand.ShutdownServer;
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
                        gameController.matchStarter.HandleMatchSettings(
                            tomlMatchSettings,
                            matchSettingsMessage,
                            !startedCommunications
                        );
                        break;
                    case DataType.MatchSettings:
                        var matchSettings = rlbot.flat.MatchSettings.GetRootAsMatchSettings(byteBuffer);
                        gameController.matchStarter.HandleMatchSettings(
                            matchSettings.UnPack(),
                            message,
                            !startedCommunications
                        );
                        break;
                    case DataType.PlayerInput:
                        var playerInputMsg = PlayerInput.GetRootAsPlayerInput(byteBuffer);
                        var carInput = FlatToModel.ToCarInput(playerInputMsg.ControllerState.Value);
                        var actorId = playerMapping.ActorIdFromPlayerIndex(playerInputMsg.PlayerIndex);
                        if (actorId.HasValue)
                        {
                            var playerInput = new RLBotSecret.Models.Control.PlayerInput()
                            {
                                actorId = actorId.Value,
                                carInput = carInput
                            };
                            gameController.playerInputSender.SendPlayerInput(playerInput);
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
                        if (!renderingIsEnabled)
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
                        sessionRenderIds[renderingGroup.Id] = renderIds;

                        // Send the render requests
                        gameController.renderingSender.Send();

                        break;
                    case DataType.RemoveRenderGroup:
                        var removeRenderGroup = rlbot
                            .flat.RemoveRenderGroup.GetRootAsRemoveRenderGroup(byteBuffer)
                            .UnPack();
                        RemoveRenderGroup(removeRenderGroup.Id);
                        break;
                    case DataType.DesiredGameState:
                        if (!stateSettingIsEnabled)
                        {
                            break;
                        }

                        var desiredGameState = DesiredGameState.GetRootAsDesiredGameState(byteBuffer).UnPack();
                        gameController.matchStarter.SetDesiredGameState(desiredGameState);
                        break;
                    default:
                        Console.WriteLine("Core got unexpected message type {0} from client.", message.type);
                        break;
                }
            }
        }

        private void SendShutdownConfirmation()
        {
            TypedPayload msg = new() { type = DataType.None, payload = new ArraySegment<byte>([1]), };
            SendPayloadToClient(msg);
        }

        private void RemoveRenderGroup(int renderGroupId)
        {
            // If a group already exists with the same id,
            // remove the old render items
            if (sessionRenderIds.ContainsKey(renderGroupId))
            {
                foreach (var oldRenderId in sessionRenderIds[renderGroupId])
                {
                    gameController.renderingSender.RemoveRenderItem(oldRenderId);
                }

                sessionRenderIds.Remove(renderGroupId);
                gameController.renderingSender.Send();
            }
        }

        private ushort? RenderItem(RenderTypeUnion renderMessage)
        {
            switch (renderMessage.Type)
            {
                case RenderType.Line3D:
                    var lineData = renderMessage.AsLine3D();
                    return gameController.renderingSender.AddLine3D(
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
                    return gameController.renderingSender.AddLine3DSeries(
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
                    return gameController.renderingSender.AddText2D(
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
                    return gameController.renderingSender.AddText3D(
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
            if (matchSettings.type != DataType.MatchSettings)
            {
                throw new Exception("Expected match settings, got " + matchSettings.type);
            }

            socketSpecWriter.Write(matchSettings);

            List<BoostPadT> boostPads = new();
            foreach (var boostPad in gameState.boostPads)
            {
                boostPads.Add(
                    new BoostPadT()
                    {
                        Location = new Vector3T()
                        {
                            X = boostPad.spawnPosition.x,
                            Y = boostPad.spawnPosition.y,
                            Z = boostPad.spawnPosition.z,
                        },
                        IsFullBoost = boostPad.isFullBoost,
                    }
                );
            }

            FieldInfoT fieldInfoT = new() { BoostPads = boostPads, Goals = gameState.goals, };

            FlatBufferBuilder builder = new(1024);
            var offset = FieldInfo.Pack(builder, fieldInfoT);
            builder.Finish(offset.Value);
            var fieldInfo = TypedPayload.FromFlatBufferBuilder(DataType.FieldInfo, builder);
            socketSpecWriter.Write(fieldInfo);

            socketSpecWriter.Send();
            Console.WriteLine("Core sent intro data to client.");
            NeedsIntroData = false;
        }

        public void ToggleStateSetting(bool isEnabled)
        {
            stateSettingIsEnabled = isEnabled;
        }

        public void ToggleRendering(bool isEnabled)
        {
            stateSettingIsEnabled = isEnabled;
        }

        internal void SendPayloadToClient(TypedPayload payload)
        {
            socketSpecWriter.Write(payload);
            socketSpecWriter.Send();
        }

        internal void SetStartCommunications(bool startedCommunications)
        {
            this.startedCommunications = startedCommunications;
        }

        internal bool HasRequestedServerStop()
        {
            if (requestedServerStop)
            {
                requestedServerStop = false;
                return true;
            }

            return false;
        }

        internal bool HasRequestedMatchStop()
        {
            if (requestedMatchStop)
            {
                requestedMatchStop = false;
                return true;
            }

            return false;
        }

        internal void SetMatchEnded()
        {
            Console.WriteLine("Setting match ended in session.");
            matchEnded = true;
        }
    }
}
