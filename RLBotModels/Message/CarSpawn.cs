using RLBotModels.Phys;

namespace RLBotModels.Message
{
    public class CarSpawn : IMessage
    {
        public ushort actorId;
        public ushort commandId;
        public string name = "";
        public byte team;
        public Hitbox hitbox;
    }
}
