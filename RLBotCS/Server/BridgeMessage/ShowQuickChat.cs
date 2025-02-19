﻿using rlbot.flat;

namespace RLBotCS.Server.BridgeMessage;

record ShowQuickChat(MatchCommT MatchComm) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context) =>
        context.QuickChat.AddChat(MatchComm, context.GameState.SecondsElapsed);
}
