using RLBotModels.Message;
using RLBotModels.Phys;

namespace RLBotCS.RLBotPacket
{
    internal class Ball
    {
        public Physics physics;
        public BallTouch latestTouch;
        public CollisionShapeUnion shape = new()
        {
            Type = CollisionShape.SphereShape,
            Value = new SphereShape()
            {
                Diameter = 91.25f,
            }
        };
        public ushort actorId;
    }
}
