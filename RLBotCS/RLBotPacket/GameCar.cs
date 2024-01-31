using RLBotModels.Phys;

namespace RLBotCS.RLBotPacket
{
    internal class GameCar
    {
        public Physics physics;
        public rlbot.flat.AirState airState;
        public float dodgeTimeout;
        public float demolishedTimeout;
        public uint lastJumpedFrame;
        public uint firstDemolishedFrame;
        public bool isSuperSonic;
        public bool isBot;
        public string name = "";
        public uint team;
        public float boost;
        public int spawnId;
        public BoxDimensions hitbox;
        public Vector3 hitboxOffset;
        public ScoreInfo scoreInfo;
    }
}
