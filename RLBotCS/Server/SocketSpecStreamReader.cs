namespace RLBotCS.Server
{
    /**
     * https://github.com/RLBot/RLBot/wiki/Sockets-Specification
     */
    static class SocketSpecStreamReader
    {
        internal static IEnumerable<TypedPayload> Read(Stream stream)
        {
            var buffer = new StreamBuffer();

            while (buffer.Read(stream))
            {
                var typedPayload = buffer.ReadTypedPayload();

                while (typedPayload.payload.Count > 0)
                {
                    yield return typedPayload;
                    typedPayload = buffer.ReadTypedPayload();
                }
            }
        }

        internal class TypedPayload
        {
            public DataType type;
            public ArraySegment<byte> payload;
        }
        
        class StreamBuffer
        {
            int messageStart;
            int bufferIndex;
            byte[] buffer;

            public StreamBuffer()
            {
                buffer = new byte[ushort.MaxValue];
            }

            internal bool Read(Stream stream)
            {
                if (bufferIndex == buffer.Length)
                {
                    if (messageStart == 0)
                    {
                        throw new InvalidDataException(string.Format("Incoming data of size {0} bytes did not fit in our buffer.", buffer.Length));
                    }

                    StartNewBuffer();
                }

                try
                {
                    int bytesRead = stream.Read(buffer, bufferIndex, buffer.Length - bufferIndex);

                    if (bytesRead == 0)
                    {
                        return false;
                    }

                    bufferIndex += bytesRead;

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            void StartNewBuffer()
            {
                // Copy remaining data to new buffer
                var newBuffer = new byte[buffer.Length];
                int copyLength = bufferIndex - messageStart;
                Array.Copy(buffer, messageStart, newBuffer, 0, copyLength);
                bufferIndex = copyLength;
                buffer = newBuffer;
                messageStart = 0;
            }

            public TypedPayload ReadTypedPayload()
            {
                // First two bytes are an integer (big-endian) which specifies the data type (see list below).
                // Next two bytes are an integer(big endian) which specifies the number of bytes in the payload.
                // https://github.com/RLBot/RLBot/wiki/Sockets-Specification
                int headerSize = sizeof(ushort) * 2;

                if (bufferIndex - messageStart < headerSize)
                {
                    return new TypedPayload() { type = DataType.None, payload = new ArraySegment<byte>() };
                }

                DataType dataType = ReadDataType();
                ushort payloadSize = ReadPayloadSize();
                int messageSize = payloadSize + headerSize;

                if (bufferIndex - messageStart < messageSize)
                {
                    return new TypedPayload() { type = DataType.None, payload = new ArraySegment<byte>() };
                }

                var payload = new ArraySegment<byte>(buffer, messageStart + headerSize, payloadSize);
                messageStart += messageSize;
                return new TypedPayload() { type = dataType, payload = payload };
            }

            ushort ReadPayloadSize()
            {
                return (ushort)((buffer[messageStart + 2] << 8) | buffer[messageStart + 3]);
            }

            DataType ReadDataType()
            {
                return (DataType)((buffer[messageStart] << 8) | buffer[messageStart + 1]);
            }
        }
    }
}
