using FlatBuffers;
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
                            Console.WriteLine("Got input from unknown player index {0}", playerInputMsg.PlayerIndex);
                        }
                        break;
                    case DataType.QuickChat:
                        break;
                    case DataType.RenderGroup:
                        break;
                    case DataType.DesiredGameState:
                        break;
                    default:
                        Console.WriteLine("Got unexpected message type {0} from client.", message.type);
                        break;
                }
            }
        }

        internal void SendPayloadToClient(TypedPayload payload)
        {
            socketSpecWriter.Write(payload);
        }
    }
}
