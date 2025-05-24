using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Server;

/**
 * Communicates with bots and scripts over TCP according to the spec
 * defined at https://wiki.rlbot.org/framework/sockets-specification/
 */
class SpecStreamWriter(Stream stream)
{
    private static readonly ILogger Logger = Logging.GetLogger("SpecStreamWriter");

    private readonly FlatBufferBuilder _messageBuilder = new(1 << 12);
    private readonly byte[] _messageBuffer = new byte[2 + ushort.MaxValue];

    private static void WriteBigEndian(ushort value, byte[] buffer)
    {
        buffer[0] = (byte)((value >> 8) & 0xFF);
        buffer[1] = (byte)(value & 0xFF);
    }

    internal void Write(CoreMessageUnion message)
    {
        var packet = new CorePacketT { Message = message };

        _messageBuilder.Clear();
        _messageBuilder.Finish(CorePacket.Pack(_messageBuilder, packet).Value);
        ArraySegment<byte> payload = _messageBuilder.DataBuffer.ToArraySegment(
            _messageBuilder.DataBuffer.Position,
            _messageBuilder.DataBuffer.Length - _messageBuilder.DataBuffer.Position
        );

        if (payload.Count > ushort.MaxValue)
        {
            // Can't send if the message size is bigger than our header can describe.
            Logger.LogError(
                $"Cannot send message because size of {payload.Count} cannot be described by a ushort."
            );
            return;
        }

        if (payload.Count == 0 || payload.Array == null)
        {
            Logger.LogWarning("Cannot send an empty message.");
            return;
        }

        WriteBigEndian((ushort)payload.Count, _messageBuffer);
        Array.Copy(payload.Array, payload.Offset, _messageBuffer, 2, payload.Count);

        stream.Write(_messageBuffer, 0, 2 + payload.Count);
    }

    internal void Send() => stream.Flush();
}
