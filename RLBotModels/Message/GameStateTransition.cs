using RLBotModels.Phys;

namespace RLBotModels.Message
{
    public class GameStateTransition : IMessage
    {
        public GameStateType gameState;
        public bool isOvertime;
    }
}
