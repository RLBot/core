using Bridge.State;

namespace RLBotCS.Server.BridgeMessage;

record RemoveOldPlayers(List<int> spawnIds) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        foreach (int spawnId in spawnIds)
        {
            PlayerMetadata? player = context
                .GameState.PlayerMapping.GetKnownPlayers()
                .FirstOrDefault(p => p.SpawnId == spawnId);

            if (player != null)
            {
                context.MatchCommandSender.AddDespawnCommand(player.ActorId);
            }
        }
    }
}