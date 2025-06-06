using Microsoft.Extensions.Logging;
using RLBot.Flat;

namespace RLBotCS.Server.BridgeMessage;

/// <summary>
/// Sets the loadout of a bot that has yet to ready up and spawn (replaces loadout generators from v4).
/// See also <see cref="StateSetLoadout"/> for loadout change during matches.
/// </summary>
record SetInitLoadout(PlayerLoadoutT Loadout, int PlayerId) : IBridgeMessage
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
                "Cannot set initial loadout of bot with player id {}. Cars have already spawned.",
                PlayerId
            );
            return;
        }

        var player = matchConfig.PlayerConfigurations.Find(p => p.PlayerId == PlayerId);
        if (player is null)
        {
            context.Logger.LogError(
                "Cannot set loadout of player with player id {}. No such player exists.",
                PlayerId
            );
            return;
        }

        var bot = player.Variety.AsCustomBot();
        bot.Loadout = Loadout;
    }
}
