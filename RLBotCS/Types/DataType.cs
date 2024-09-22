namespace RLBotCS.Types;

/**
 * https://wiki.rlbot.org/framework/sockets-specification/#data-types
 */
public enum DataType : ushort
{
    None,

    /// <summary>
    /// Arrives at a high rate according to https://wiki.rlbot.org/botmaking/tick-rate/ except
    /// "desired tick rate" is not relevant here
    /// </summary>
    GameTickPacket,

    /// <summary>
    /// Sent once when a match starts, or when you first connect.
    /// </summary>
    FieldInfo,

    /// <summary>
    /// Sent once when a match starts, or when you first connect.
    /// </summary>
    StartCommand,
    MatchSettings,
    PlayerInput,
    DesiredGameState,
    RenderGroup,
    RemoveRenderGroup,
    MatchComms,

    /// <summary>
    /// Sent every time the ball diverges from the previous prediction,
    /// or when the previous prediction no longer gives a full 6 seconds into the future
    /// </summary>
    BallPrediction,

    /// <summary>
    /// Clients must send this after connecting to the socket.
    /// </summary>
    ConnectionSettings,

    /// <summary>
    /// used to end a match and shut down bots (optionally the server as well)
    /// </summary>
    StopCommand,

    /// <summary>
    /// Use to dynamically set the loadout of a bot
    /// </summary>
    SetLoadout,

    /// <summary>
    /// Indicates that a connection is ready to receive `GameTickPacket`s
    /// </summary>
    InitComplete
}
