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

        private bool ready;
        public bool needsIntroData;

        private bool wantsBallPredictions;
        private bool wantsGameMessages;
        private bool wantsQuickChat;

        public FlatbufferSession(Stream stream, GameController gameController, PlayerMapping playerMapping)
        {
            this.stream = stream;
            this.gameController = gameController;
            this.playerMapping = playerMapping;
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
                        ready = true;
                        needsIntroData = true;
                        wantsBallPredictions = readyMsg.WantsBallPredictions;
                        wantsGameMessages = readyMsg.WantsGameMessages;
                        wantsQuickChat = readyMsg.WantsQuickChat;
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
    }
}
