using RLBotModels.Phys;

namespace RLBotModels.Message
{
    public class PhysicsUpdate : IMessage
    {
        public Dictionary<ushort, CarPhysics> carUpdates = new();
        public Physics? ballUpdate;
    }
}
