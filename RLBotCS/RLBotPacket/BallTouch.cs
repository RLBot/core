using RLBotModels.Phys;

namespace RLBotCS.RLBotPacket
{
    internal struct BallTouch
    {
        public string playerName;
        public float timeSeconds;
        public Vector3 hitLocation;
        public Vector3 hitNormal;
        public byte team;
        public int playerIndex;
    }
}
