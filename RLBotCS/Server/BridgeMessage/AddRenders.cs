using RLBot.Flat;

namespace RLBotCS.Server.BridgeMessage;

readonly struct AddRenders(int ClientId, int RenderId, List<RenderMessageT> RenderItems)
    : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        lock (context)
            context.RenderingMgmt.AddRenderGroup(
                ClientId,
                RenderId,
                RenderItems,
                context.GameState
            );
    }
}
