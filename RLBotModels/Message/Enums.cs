namespace RLBotModels.Message
{

    public enum GameMode : byte
    {
        Unknown = 0,
        Soccar = 1,
        Hoops = 2,
        Snowday = 3,
        Rumble = 4,
        Dropshot = 5,
    };

    /// What state the game/match is in.
    public enum GameStateType : byte
    {
        /// Game has not been created yet
        Inactive = 0,
        /// 3-2-1 countdown
        Countdown = 1,
        /// After countdown, but before ball has been hit
        Kickoff = 2,
        /// Ball has been hit
        Active = 3,
        /// A goal was scored. Waiting for replay.
        GoalScored = 4,
        /// Watching replay
        Replay = 5,
        /// Game paused
        Paused = 6,
        /// Match has ended
        Ended = 7,
    };

    public enum BoostPadState : byte
    {
        Available = 0,
        PickedUp = 1,
    };

    /// What the player's car is currently doing
    public enum CarState : byte
    {
        /// Wheels on ground
        OnGround = 0,
        /// Did a jump. Changes to InAir when no longer jumping.
        Jumping = 1,
        /// Did a double jump. Changes to InAir immediately after.
        DoubleJumping = 2,
        /// Did a dodge. Changes to InAir when dodge completes
        Dodged = 3,
        /// Wheels not on ground
        InAir = 4,
        /// Demolished, awaiting respawn
        Demolished = 5,
    };

    public enum BotSkill : byte
    {
        Intro = 0,
        Easy = 1,
        Medium = 2,
        Hard = 3,
        Custom = 4,
    };

    public enum TextHAlign : byte
    {
        Left = 0,
        Center = 1,
        Right = 2,
    };

    public enum TextVAlign : byte
    {
        Top = 0,
        Center = 1,
        Bottom = 2,
    };
}
