﻿using Google.FlatBuffers;
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

        public void Close() {
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
                        break;
                    case DataType.DesiredGameState:
                        break;
                    default:
                        Console.WriteLine("Core got unexpected message type {0} from client.", message.type);
                        break;
                }
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

        internal void SendPayloadToClient(TypedPayload payload)
        {
            socketSpecWriter.Write(payload);
        }
    }
}
