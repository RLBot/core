using Google.FlatBuffers;
using rlbot.flat;
using RLBotCS.GameControl;
using RLBotCS.GameState;
using RLBotSecret.Conversion;

namespace RLBotCS.Server
{

    /**
     * Taken from https://codinginfinite.com/multi-threaded-tcp-server-core-example-csharp/
     */
    internal class FlatbufferSession
    {
        private Stream stream;
        private GameController gameController;
        private PlayerMapping playerMapping;
        private SocketSpecStreamWriter socketSpecWriter;
        private Dictionary<int, List<ushort>> sessionRenderIds = new();
        private ushort? ballActorId;

        public bool IsReady
        { get; private set; }

        public bool NeedsIntroData
        { get; private set; }

        public bool WantsBallPredictions
        { get; private set; }

        public bool WantsGameMessages
        { get; private set; }

        public bool WantsQuickChat
        { get; private set; }

        public FlatbufferSession(Stream stream, GameController gameController, PlayerMapping playerMapping)
        {
            this.stream = stream;
            this.gameController = gameController;
            this.playerMapping = playerMapping;
            this.socketSpecWriter = new SocketSpecStreamWriter(stream);
        }

        public void RemoveRenders()
        {
            foreach (var renderIds in sessionRenderIds.Values)
            {
                foreach (var renderId in renderIds)
                {
                    gameController.renderingSender.RemoveRenderItem(renderId);
                }
            }

            sessionRenderIds.Clear();
        }

        public void Close() {
            RemoveRenders();
            stream.Close();
        }

        public void RunBlocking()
        {
            foreach (var message in SocketSpecStreamReader.Read(stream))
            {
                var byteBuffer = new ByteBuffer(message.payload.Array, message.payload.Offset);

                switch (message.type)
                {
                    case DataType.ReadyMessage:
                        var readyMsg = ReadyMessage.GetRootAsReadyMessage(byteBuffer);
                        IsReady = true;
                        NeedsIntroData = true;
                        WantsBallPredictions = readyMsg.WantsBallPredictions;
                        WantsGameMessages = readyMsg.WantsGameMessages;
                        WantsQuickChat = readyMsg.WantsQuickChat;
                        break;
                    case DataType.MatchSettings:
                        var matchSettings = rlbot.flat.MatchSettings.GetRootAsMatchSettings(byteBuffer);
                        gameController.matchStarter.HandleMatchSettings(matchSettings.UnPack(), message);
                        break;
                    case DataType.PlayerInput:
                        var playerInputMsg = PlayerInput.GetRootAsPlayerInput(byteBuffer);
                        var carInput = FlatToModel.ToCarInput(playerInputMsg.ControllerState.Value);
                        var actorId = playerMapping.ActorIdFromPlayerIndex(playerInputMsg.PlayerIndex);
                        if (actorId.HasValue)
                        {
                            var playerInput = new RLBotModels.Control.PlayerInput() { actorId = actorId.Value, carInput = carInput };
                            gameController.playerInputSender.SendPlayerInput(playerInput);
                        }
                        else
                        {
                            Console.WriteLine("Core got input from unknown player index {0}", playerInputMsg.PlayerIndex);
                        }
                        break;
                    case DataType.QuickChat:
                        break;
                    case DataType.RenderGroup:
                        var renderingGroup = RenderGroup.GetRootAsRenderGroup(byteBuffer).UnPack();
                        List<ushort> renderIds = new();

                        // Create render requests
                        foreach (var renderMessage in renderingGroup.RenderMessages)
                        {
                            if (RenderItem(renderMessage.Variety) is ushort renderId)
                            {
                                renderIds.Add(renderId);
                            }
                        }

                        RemoveRenderGroup(renderingGroup.Id);

                        // Add the new render items to the tracker
                        sessionRenderIds[renderingGroup.Id] = renderIds;

                        // Send the render requests
                        gameController.renderingSender.Send();
                        break;
                    case DataType.RemoveRenderGroup:
                        var removeRenderGroup = rlbot.flat.RemoveRenderGroup.GetRootAsRemoveRenderGroup(byteBuffer).UnPack();
                        RemoveRenderGroup(removeRenderGroup.Id);
                        break;
                    case DataType.DesiredGameState:
                        var desiredGameState = DesiredGameState.GetRootAsDesiredGameState(byteBuffer).UnPack();
                        gameController.matchStarter.SetDesiredGameState(desiredGameState, ballActorId);
                        break;
                    default:
                        Console.WriteLine("Core got unexpected message type {0} from client.", message.type);
                        break;
                }
            }
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
            }
        }


        private ushort? RenderItem(RenderTypeUnion renderMessage)
        {
            switch (renderMessage.Type)
            {
                case RenderType.Line3D:
                    var lineData = renderMessage.AsLine3D();
                    return gameController.renderingSender.AddLine3D(
                        new RLBotModels.Phys.Vector3()
                        {
                            x = lineData.Start.X,
                            y = lineData.Start.Y,
                            z = lineData.Start.Z,
                        },
                        new RLBotModels.Phys.Vector3()
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
                    List<RLBotModels.Phys.Vector3> points = new();
                    foreach (var point in polyLineData.Points)
                    {
                        points.Add(new RLBotModels.Phys.Vector3()
                        {
                            x = point.X,
                            y = point.Y,
                            z = point.Z,
                        });
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
                        new RLBotModels.Phys.Vector3()
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


        public void SendIntroData(TypedPayload matchSettings, GameState.GameState gameState)
        {
            if (matchSettings.type != DataType.MatchSettings)
            {
                throw new Exception("Expected match settings, got " + matchSettings.type);
            }

            Console.WriteLine("Core sent intro data to client.");
            socketSpecWriter.Write(matchSettings);

            List<BoostPadT> boostPads = new();
            foreach (var boostPad in gameState.boostPads)
            {
                boostPads.Add(new BoostPadT()
                {
                    Location = new Vector3T() {
                        X = boostPad.spawnPosition.x,
                        Y = boostPad.spawnPosition.y,
                        Z = boostPad.spawnPosition.z,
                    },
                    IsFullBoost = boostPad.isFullBoost,
                });
            }

            // TODO: Add goals
            FieldInfoT fieldInfoT = new()
            {
                BoostPads = boostPads,
            };

            FlatBufferBuilder builder = new(1024);
            var offset = FieldInfo.Pack(builder, fieldInfoT);
            builder.Finish(offset.Value);
            var fieldInfo = TypedPayload.FromFlatBufferBuilder(DataType.FieldInfo, builder);
            socketSpecWriter.Write(fieldInfo);

            NeedsIntroData = false;
        }

        public void SetBallActorId(ushort actorId)
        {
            ballActorId = actorId;
        }

        internal void SendPayloadToClient(TypedPayload payload)
        {
            socketSpecWriter.Write(payload);
        }
    }
}
