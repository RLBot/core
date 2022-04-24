using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLBotCS.Server
{
    /**
     * https://github.com/RLBot/RLBot/wiki/Sockets-Specification#data-types
     */
    enum DataType : ushort
    {
        None,
        // Arrives at a high rate according to https://github.com/RLBot/RLBot/wiki/Tick-Rate except
        // "desired tick rate" is not relevant here
        GameTickPacket,
        // Sent once when a match starts, or when you first connect.
        FieldInfo,
        // Sent once when a match starts, or when you first connect.
        MatchSettings,
        PlayerInput,
        // Deprecated, related to https://github.com/RLBot/RLBot/wiki/Remote-RLBot
        ActorMappingData,
        // Deprecated, related to https://github.com/RLBot/RLBot/wiki/Remote-RLBot.
        ComputerId,
        DesiredGameState,
        RenderGroup,
        QuickChat,
        // Sent every time the ball diverges from the previous prediction,
        // or when the previous prediction no longer gives a full 6 seconds into the future
        BallPrediction,
        // Clients must send this after connecting to the socket.
        ReadyMessage,
        // List of messages, having one of several possible sub-types.
        MessagePacket
    };
}
