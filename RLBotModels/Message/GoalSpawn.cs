using RLBotModels.Phys;

namespace RLBotModels.Message
{
    public class GoalSpawn : IMessage
    {
        public ushort actorId;
        public byte team;
        public Vector3 location;
        public Vector3 direction;
        public float width;
        public float height;
    }
    
}