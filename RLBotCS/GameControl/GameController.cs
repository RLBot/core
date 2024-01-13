using RLBotSecret.Controller;

namespace RLBotCS.GameControl
{
    internal class GameController
    {
        public PlayerInputSender playerInputSender;
        public RenderingSender renderingSender;
        public MatchStarter matchStarter;

        public GameController(
            PlayerInputSender playerInputSender,
            RenderingSender renderingSender,
            MatchStarter matchStarter
        )
        {
            this.playerInputSender = playerInputSender;
            this.renderingSender = renderingSender;
            this.matchStarter = matchStarter;
        }
    }
}
