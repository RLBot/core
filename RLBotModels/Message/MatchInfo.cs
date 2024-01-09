using RLBotModels.Phys;

namespace RLBotModels.Message
{
    /**
     * This message arrives when a new match starts. It will also be sent right after RLBot connects
     * to Rocket League if there's a match already in progress.
     */
    public class MatchInfo : IMessage
    {
        public string mapName;
        public GameMode mode;
        public Vector3 gravity;

        public MatchInfo(string mapName)
        {
            this.mapName = mapName;
        }
    }
}
