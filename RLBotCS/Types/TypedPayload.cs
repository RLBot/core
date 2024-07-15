using Google.FlatBuffers;

namespace RLBotCS.Types;

public class TypedPayload
{
    public DataType Type;
    public ArraySegment<byte> Payload;

    public static TypedPayload FromFlatBufferBuilder(DataType type, FlatBufferBuilder builder)
    {
        return new TypedPayload
        {
            Type = type,
            Payload = builder.DataBuffer.ToArraySegment(
                builder.DataBuffer.Position,
                builder.DataBuffer.Length - builder.DataBuffer.Position
            )
        };
    }
}
