using Bridge.State;
using rlbot.flat;

namespace RLBotCS.Server.BridgeMessage;

record SpawnHuman(PlayerConfigurationT Config, uint DesiredIndex) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.QueueConsoleCommand("ChangeTeam " + Config.Team);

        PlayerMetadata? alreadySpawnedPlayer = context
            .GameState.PlayerMapping.GetKnownPlayers()
            .FirstOrDefault(kp => Config.SpawnId == kp.SpawnId);
        if (alreadySpawnedPlayer != null)
        {
            alreadySpawnedPlayer.PlayerIndex = DesiredIndex;
            return;
        }

        context.GameState.PlayerMapping.AddPendingSpawn(
            new SpawnTracker
            {
                CommandId = 0,
                SpawnId = Config.SpawnId,
                DesiredPlayerIndex = DesiredIndex,
                IsBot = false,
                IsCustomBot = false,
            }
        );
    }
}
