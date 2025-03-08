namespace RLBotCS.Server.BridgeMessage;

/// <summary>
/// A message sent to the <see cref="BridgeHandler"/>.
/// </summary>
interface IBridgeMessage
{
    public void HandleMessage(BridgeContext context);
}
