using FlatBuffers;

namespace RLBotCS.Server
{
    internal class TypedPayload
    {
        public DataType type;
        public ArraySegment<byte> payload;

        internal static TypedPayload FromFlatBufferBuilder(DataType type, FlatBufferBuilder builder)
        {
            return new TypedPayload()
            {
                type = type,
                payload = builder.DataBuffer.ToArraySegment(builder.DataBuffer.Position, builder.DataBuffer.Length - builder.DataBuffer.Position)
            };
        }
    }
}
