using RLBotCS.RLBotPacket;
using RLBotModels.Message;

namespace RLBotCS.GameState
{
    internal class MatchSettings
    {
        public GameTickPacket gameTickPacket = new();

        public PlayerMapping playerMapping = new();

    }
}
