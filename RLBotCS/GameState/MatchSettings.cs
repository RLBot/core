using RLBotCS.RLBotPacket;

namespace RLBotCS.GameState
{
    internal class MatchSettings
    {
        public GameTickPacket gameTickPacket = new();

        public PlayerMapping playerMapping = new();
    }
}
