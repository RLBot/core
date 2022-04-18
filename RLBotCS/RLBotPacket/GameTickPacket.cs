namespace RLBotCS.RLBotPacket
{
    internal class GameTickPacket
    {
        public SortedDictionary<int, GameCar> gameCars = new();
        public List<BoostPadStatus> gameBoosts = new();
        public Ball ball = new();
        // TODO: add gameInfo and teams fields.
    }
}