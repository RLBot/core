using rlbot.flat;

namespace RLBotCS.Server.BridgeMessage;

record ShowQuickChat(MatchCommT MatchComm) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        if (context.GameState.GameCars.TryGetValue(MatchComm.Index, out var car))
            context.QuickChat.AddChat(MatchComm, car.Name, context.GameState.SecondsElapsed);
    }
}
