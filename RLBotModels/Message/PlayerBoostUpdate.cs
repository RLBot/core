using RLBotModels.Phys;

namespace RLBotModels.Message
{
    public class PlayerBoostUpdate : IMessage
    {
        public ushort actorId;

        public bool isBoosting;

        /// <summary>
        /// Integer number out of 100.
        /// </summary>
        public ushort boostRemaining;
    }
}
