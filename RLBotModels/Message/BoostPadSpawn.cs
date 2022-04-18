using RLBotModels.Phys;

namespace RLBotModels.Message
{
    public class BoostPadSpawn : IMessage
    {
        public ushort actorId;
        public Vector3 spawnPosition;
        public bool isFullBoost;
    }
}
