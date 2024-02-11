using RLBotSecret.Controller;

namespace RLBotCS.GameControl
{
    internal class GameController
    {
        public PlayerInputSender PlayerInputSender;
        public RenderingSender RenderingSender;
        public MatchStarter MatchStarter;

        public GameController(
            PlayerInputSender playerInputSender,
            RenderingSender renderingSender,
            MatchStarter matchStarter
        )
        {
            this.PlayerInputSender = playerInputSender;
            this.RenderingSender = renderingSender;
            this.MatchStarter = matchStarter;
        }
    }
}
