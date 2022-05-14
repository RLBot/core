using RLBotSecret.Controller;

namespace RLBotCS.GameControl
{
    internal class GameController
    {
        public PlayerInputSender playerInputSender;
        public RenderingSender renderingSender;

        public GameController(PlayerInputSender playerInputSender, RenderingSender renderingSender)
        {
            this.playerInputSender = playerInputSender;
            this.renderingSender = renderingSender;
        }
    }
}
