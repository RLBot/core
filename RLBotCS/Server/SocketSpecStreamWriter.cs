using static RLBotCS.Server.SocketSpecStreamReader;

namespace RLBotCS.Server
{
    /**
	 * Communicates with bots and scripts over TCP according to the spec
	 * defined at https://github.com/RLBot/RLBot/wiki/Sockets-Specification
	 */
    internal class SocketSpecStreamWriter
    {
        Stream stream;
        byte[] dataTypeBuffer = new byte[2];
        byte[] messageLengthBuffer = new byte[2];

        public SocketSpecStreamWriter(Stream stream)
        {
            this.stream = stream;
        }

        void PrepareMessageLength(ushort length)
        {
            WriteBigEndian(length, messageLengthBuffer);
        }

        void PrepareDataType(DataType dataType)
        {
            WriteBigEndian((ushort)dataType, dataTypeBuffer);
        }

        void WriteBigEndian(ushort value, byte[] buffer)
        {
            buffer[1] = (byte)((value >> 8) & 0xFF);
            buffer[0] = (byte)((value) & 0xFF);
        }

        internal void Write(TypedPayload message)
        {
            if (message.payload.Count > ushort.MaxValue)
            {
                // Can't send if the message size is bigger than our header can describe.
                Console.WriteLine("Warning! Cannot send message because size of {0} cannot be described by a ushort.", message.payload.Count);
                return;
            }
            PrepareDataType(message.type);
            PrepareMessageLength((ushort)message.payload.Count);

            stream.Write(dataTypeBuffer, 0, dataTypeBuffer.Length);
            stream.Write(messageLengthBuffer, 0, messageLengthBuffer.Length);
            stream.Write(message.payload.Array, message.payload.Offset, message.payload.Count);
        }
    }
}
