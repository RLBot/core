namespace RLBotCS.Server.BridgeMessage;

internal record AddPerfSample(uint Index, bool GotInput) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        if (context.GameState.GameCars.TryGetValue(Index, out var car))
            context.PerfMonitor.AddSample(car.Name, GotInput);
    }
}