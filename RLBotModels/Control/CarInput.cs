namespace RLBotModels.Control
{
    public struct CarInput
    {
        public float throttle, steer;
        public float pitch, yaw, roll;
        public float dodgeForward, dodgeStrafe;
        public bool jump;
        public bool boost;
        public bool handbrake;
        public bool useItem;
    }
}
