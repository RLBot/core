using RLBotSecret.Types;
using System.Net.Sockets;

namespace RLBotCS.Server
{
    /**
     * https://wiki.rlbot.org/framework/sockets-specification/
     */
    static class SocketSpecStreamReader
    {
        internal static IEnumerable<TypedPayload> Read(NetworkStream stream)
        {
            var buffer = new BufferedStream(stream, ushort.MaxValue);

            var ushortReader = new byte[2];
            var payloadReader = new byte[ushort.MaxValue];

            // block until we get a message, then yield it
            // stop blocking when the stream is closed
            while (true)
            {
                var ushortReaderSpan = new Span<byte>(ushortReader);

                buffer.ReadExactly(ushortReaderSpan);
                var dataType = ReadDataType(ushortReaderSpan);

                buffer.ReadExactly(ushortReaderSpan);
                var payloadSize = ReadPayloadSize(ushortReaderSpan);

                var payloadReaderSpan = new Span<byte>(payloadReader, 0, payloadSize);
                buffer.ReadExactly(payloadReaderSpan);

                yield return new() { type = dataType, payload = new(payloadReader, 0, payloadSize) };
            }
        }

        private static DataType ReadDataType(Span<byte> bytes)
        {
            return (DataType)((bytes[0] << 8) | bytes[1]);
        }

        private static ushort ReadPayloadSize(Span<byte> bytes)
        {
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }
    }
}
