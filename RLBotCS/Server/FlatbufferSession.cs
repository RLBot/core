using FlatBuffers;
using rlbot.flat;

namespace RLBotCS.Server
{

    /**
     * Taken from https://codinginfinite.com/multi-threaded-tcp-server-core-example-csharp/
     */
    internal class FlatbufferSession
    {

        private Stream stream;

        private bool ready;
        public bool needsIntroData;

        private bool wantsBallPredictions;
        private bool wantsGameMessages;
        private bool wantsQuickChat;

        public FlatbufferSession(Stream stream)
        {
            this.stream = stream;
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
