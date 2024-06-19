using System.Net.Sockets;
using RLBotSecret.Types;

namespace RLBotCS.Server;

/**
 * https://wiki.rlbot.org/framework/sockets-specification/
 */
class SocketSpecStreamReader
{
    private BufferedStream _bufferedStream;
    private byte[] _ushortReader = new byte[2];
    private byte[] _payloadReader = new byte[ushort.MaxValue];

    public SocketSpecStreamReader(NetworkStream stream)
    {
        _bufferedStream = new BufferedStream(stream, 4 + ushort.MaxValue);
    }

    internal async IAsyncEnumerable<TypedPayload> ReadAllAsync()
    {
        while (true)
        {
            DataType dataType;
            ushort payloadSize;

            try
            {
                await _bufferedStream.ReadExactlyAsync(_ushortReader);
                dataType = ReadDataType(_ushortReader);

                await _bufferedStream.ReadExactlyAsync(_ushortReader);
                payloadSize = ReadPayloadSize(_ushortReader);

                await _bufferedStream.ReadExactlyAsync(_payloadReader, 0, payloadSize);
            }
            catch (Exception)
            {
                break;
            }

            yield return new() { Type = dataType, Payload = new(_payloadReader, 0, payloadSize) };
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
