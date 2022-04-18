using RLBotModels.Phys;

namespace RLBotModels.Message
{
    public class PlayerStatsUpdate : IMessage
    {
        public ushort actorId;
        public ushort score;
        public byte goals;
        public byte ownGoals;
        public byte assists;
        public byte saves;
        public byte shots;
        public byte demolitions;
    }
}
