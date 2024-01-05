using RLBotModels.Phys;

namespace RLBotCS.RLBotPacket
{
    internal class GameCar
    {
        public Physics physics;
        public bool isDemolished;
        public bool hasWheelContact;
        public bool isSuperSonic;
        public bool isBot;
        public bool jumped;
        public bool doubleJumped;
        public string name = "";
        public int team;
        public float boost;
        public int spawnId;
        public BoxDimensions hitbox;
        public Vector3 hitboxOffset;
        public ScoreInfo scoreInfo;
    }
}
