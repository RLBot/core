using Microsoft.Extensions.Logging;
using rlbot.flat;

namespace RLBotCS.Server.BridgeMessage;

/// <summary>
/// Sets the loadout of a bot that has yet to ready up and spawn (replaces loadout generators from v4).
/// See also <see cref="StateSetLoadout"/> for loadout change during matches.
/// </summary>
record SetInitLoadout(PlayerLoadoutT Loadout, int SpawnId) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        var matchConfig = context.MatchConfig;
        if (matchConfig is null)
        {
            context.WaitingInitLoadouts.Add(this);
            return;
        }

        if (context.MatchStarter.HasSpawnedCars)
        {
            // BUG: If "wait_for_agents=false" then you cannot use the new 'loadout generators'
            context.Logger.LogWarning(
                "Cannot set initial loadout of bot with spawn id {}. Cars have already spawned.",
                SpawnId
            );
            return;
        }

        var player = matchConfig.PlayerConfigurations.Find(p => p.SpawnId == SpawnId);
        if (player is null)
        {
            context.Logger.LogError(
                "Cannot set loadout of player with spawn id {}. No such player exists.",
                SpawnId
            );
            return;
        }

        player.Loadout = Loadout;
    }
}
