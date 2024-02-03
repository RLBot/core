namespace RLBotModels.Message
{
    public class BoostPadUpdate : IMessage
    {
        public ushort actorId;
        public BoostPadState state;
    }
}
