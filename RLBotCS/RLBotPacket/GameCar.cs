using RLBotModels.Phys;

namespace RLBotCS.RLBotPacket
{
    internal class GameCar
    {
        public Physics physics;
        public rlbot.flat.AirState airState;
        public float dodgeTimeout;
        public float demolishedTimeout;
        public int lastJumpedFrame;
        public int firstDemolishedFrame;
        public bool isSuperSonic;
        public bool isBot;
        public string name = "";
        public int team;
        public float boost;
        public int spawnId;
        public BoxDimensions hitbox;
        public Vector3 hitboxOffset;
        public ScoreInfo scoreInfo;
    }
}
