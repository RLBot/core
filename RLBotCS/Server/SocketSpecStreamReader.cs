using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using RLBotSecret.Types;

namespace RLBotCS.Server
{
    /**
     * https://wiki.rlbot.org/framework/sockets-specification/
     */
    class SocketSpecStreamReader
    {
        private TcpClient _client;
        private BufferedStream _bufferedStream;
        private byte[] _ushortReader = new byte[2];
        private byte[] _payloadReader = new byte[ushort.MaxValue];

        public SocketSpecStreamReader(TcpClient client)
        {
            _client = client;
            _bufferedStream = new BufferedStream(client.GetStream(), 4 + ushort.MaxValue);
        }

        public bool TryRead([MaybeNullWhen(false)] out TypedPayload item)
        {
            if (_client.Available == 0)
            {
                item = null;
                return false;
            }

            var ushortReaderSpan = new Span<byte>(_ushortReader);

            _bufferedStream.ReadExactly(ushortReaderSpan);
            var dataType = ReadDataType(ushortReaderSpan);

            _bufferedStream.ReadExactly(ushortReaderSpan);
            var payloadSize = ReadPayloadSize(ushortReaderSpan);

            var payloadReaderSpan = new Span<byte>(_payloadReader, 0, payloadSize);
            _bufferedStream.ReadExactly(payloadReaderSpan);

            item = new() { Type = dataType, Payload = new(_payloadReader, 0, payloadSize) };
            return true;
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
