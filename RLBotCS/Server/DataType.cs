namespace RLBotCS.Server
{
    /**
     * https://wiki.rlbot.org/framework/sockets-specification/#data-types
     */
    enum DataType : ushort
    {
        None,

        // Arrives at a high rate according to https://wiki.rlbot.org/botmaking/tick-rate/ except
        // "desired tick rate" is not relevant here
        GameTickPacket,

        // Sent once when a match starts, or when you first connect.
        FieldInfo,

        // Sent once when a match starts, or when you first connect.
        StartCommand,
        MatchSettings,
        PlayerInput,
        DesiredGameState,
        RenderGroup,
        RemoveRenderGroup,
        MatchComms,

        // Sent every time the ball diverges from the previous prediction,
        // or when the previous prediction no longer gives a full 6 seconds into the future
        BallPrediction,

        // Clients must send this after connecting to the socket.
        ReadyMessage,

        // List of messages, having one of several possible sub-types.
        MessagePacket,

        // used to end a match and shut down bots (optionally the server as well)
        StopCommand
    };
}
