using Google.FlatBuffers;
using RLBotCS.Server;

namespace RLBotCS.RLBotPacket
{
    internal class GameTickPacket
    {
        public SortedDictionary<int, GameCar> gameCars = new();
        public List<BoostPadStatus> gameBoosts = new();
        public Ball ball = new();


        // TODO: add gameInfo and teams fields.

        internal TypedPayload ToFlatbuffer()
        {
            FlatBufferBuilder builder = new(1000);
            rlbot.flat.GameTickPacket.StartGameTickPacket(builder);
            // TODO: add all the data
            var gtp = rlbot.flat.GameTickPacket.EndGameTickPacket(builder);
            builder.Finish(gtp.Value);

            return TypedPayload.FromFlatBufferBuilder(DataType.GameTickPacket, builder);
        }
    }
}