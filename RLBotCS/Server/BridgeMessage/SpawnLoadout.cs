using rlbot.flat;

namespace RLBotCS.Server.BridgeMessage;

record SpawnLoadout(PlayerLoadoutT Loadout, int SpawnId) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.MatchStarter.AddLoadout(Loadout, SpawnId);
    }
}
