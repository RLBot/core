namespace RLBotCS.GameState
{
    internal class PlayerMetadata
    {
        /// <summary>
        /// The identifier for this player's car inside RocketLeague.exe.
        /// </summary>
        public ushort actorId;

        /// <summary>
        /// The spawnId is a unique identifier chosen by a client when making a request
        /// to spawn a car. We will pass it back in the output so the client can see which
        /// spawned car corresponds to their request.
        /// </summary>
        public int? spawnId;

        /// <summary>
        /// The index of this car in the GameTickPacket's car list.
        /// </summary>
        public int playerIndex;

        /// <summary>
        /// True if it's a custom bot or Psyonix bot, false if human.
        /// </summary>
        public bool isBot;

        /// <summary>
        /// True if custom code will be in control of this car. False if it's a human or a
        /// standard Psyonix bot.
        /// </summary>
        public bool isCustomBot;
    }
}
