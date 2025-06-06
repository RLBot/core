using System.Net.Sockets;
using Google.FlatBuffers;
using RLBot.Flat;

namespace RLBotCS.Server;

/**
 * https://wiki.rlbot.org/framework/sockets-specification/
 */
class SpecStreamReader
{
    private BufferedStream _bufferedStream;
    private byte[] _ushortReader = new byte[2];
    private byte[] _payloadReader = new byte[ushort.MaxValue];

    public SpecStreamReader(NetworkStream stream)
    {
        _bufferedStream = new BufferedStream(stream, 2 + ushort.MaxValue);
    }

    internal async IAsyncEnumerable<InterfacePacket> ReadAllAsync()
    {
        while (true)
        {
            ushort payloadSize;

            try
            {
                await _bufferedStream.ReadExactlyAsync(_ushortReader);
                payloadSize = ReadBigEndian(_ushortReader);

                await _bufferedStream.ReadExactlyAsync(_payloadReader, 0, payloadSize);
            }
            catch (Exception)
            {
                break;
            }

            ByteBuffer byteBuffer = new(_payloadReader, 0);
            yield return InterfacePacket.GetRootAsInterfacePacket(byteBuffer);
        }
    }

    private static ushort ReadBigEndian(Span<byte> bytes)
    {
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }
}
