using RLBotModels.Phys;

namespace RLBotModels.Message
{
    public class BallTouch : IMessage
    {
        public ushort actorId;
        public Vector3 location;
        public Vector3 normal;
    }
}
