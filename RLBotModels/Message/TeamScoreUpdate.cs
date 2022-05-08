namespace RLBotModels.Message
{
    public class TeamScoreUpdate : IMessage
    {
        public ushort team;
        public ushort score;
    }
}
