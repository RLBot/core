using Bridge.Types;

namespace RLBotCS.Server
{
    /**
     * Communicates with bots and scripts over TCP according to the spec
     * defined at https://wiki.rlbot.org/framework/sockets-specification/
     */
    internal class SocketSpecStreamWriter
    {
        Stream _stream;
        byte[] _dataTypeBuffer = new byte[2];
        byte[] _messageLengthBuffer = new byte[2];
        byte[] _messageBuffer = new byte[4 + ushort.MaxValue];

        public SocketSpecStreamWriter(Stream stream)
        {
            this._stream = stream;
        }

        void PrepareMessageLength(ushort length)
        {
            WriteBigEndian(length, _messageLengthBuffer);
        }

        void PrepareDataType(DataType dataType)
        {
            WriteBigEndian((ushort)dataType, _dataTypeBuffer);
        }

        void WriteBigEndian(ushort value, byte[] buffer)
        {
            buffer[0] = (byte)((value >> 8) & 0xFF);
            buffer[1] = (byte)((value) & 0xFF);
        }

        internal async Task WriteAsync(TypedPayload message)
        {
            if (message.Payload.Count > ushort.MaxValue)
            {
                // Can't send if the message size is bigger than our header can describe.
                Console.WriteLine(
                    "Warning! Core cannot send message because size of {0} cannot be described by a ushort.",
                    message.Payload.Count
                );
                return;
            }

            PrepareDataType(message.Type);
            PrepareMessageLength((ushort)message.Payload.Count);

            Array.Copy(_dataTypeBuffer, 0, _messageBuffer, 0, 2);
            Array.Copy(_messageLengthBuffer, 0, _messageBuffer, 2, 2);
            Array.Copy(message.Payload.Array, message.Payload.Offset, _messageBuffer, 4, message.Payload.Count);
            await _stream.WriteAsync(_messageBuffer, 0, 4 + message.Payload.Count);
        }

        internal async Task SendAsync()
        {
            await _stream.FlushAsync();
        }
    }
}
