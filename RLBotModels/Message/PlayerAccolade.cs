namespace RLBotModels.Message
{
    public class PlayerAccolade : IMessage
    {
        public ushort actorId;

        /// <summary>
        /// Known possible values:
        ///
        /// Win, Loss, TimePlayed, Shot, Assist, Center, Clear, PoolShot,
        /// Goal, AerialGoal, BicycleGoal, BulletGoal, BackwardsGoal, LongGoal, OvertimeGoal, TurtleGoal,
        /// AerialHit, BicycleHit, BulletHit, JuggleHit, FirstTouch, BallHit,
        /// Save, EpicSave, FreezeSave, HatTrick, Savior, Playmaker, MVP,
        /// FastestGoal, SlowestGoal, FurthestGoal, OwnGoal, MostBallTouches, FewestBallTouches,
        /// MostBoostPickups, FewestBoostPickups, BoostPickups, CarTouches, Demolition, Demolish,
        /// LowFive, HighFive;
        /// </summary>
        public string accolade = "";
    }
}
