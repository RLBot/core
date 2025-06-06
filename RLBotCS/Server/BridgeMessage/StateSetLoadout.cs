using Bridge.Models.Command;
using Bridge.Models.Message;
using Bridge.State;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBotCS.Conversion;

namespace RLBotCS.Server.BridgeMessage;

record StateSetLoadout(PlayerLoadoutT Loadout, uint Index) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        PlayerMetadata? meta = context
            .GameState.PlayerMapping.GetKnownPlayers()
            .FirstOrDefault(kp => Index == kp.PlayerIndex);
        if (meta == null)
            return;

        // todo: Psyonix bot support -
        // requires additional work to figure out the proper BotSkill
        if (!meta.IsCustomBot)
        {
            context.Logger.LogWarning(
                "Player {0} is not controlled by RLBot, cannot set loadout.",
                meta.PlayerIndex
            );
            return;
        }

        context.SpawnCommandQueue.AddDespawnCommand(meta.ActorId);

        Loadout.LoadoutPaint ??= new LoadoutPaintT();

        var player = context.GameState.GameCars[meta.PlayerIndex];
        Loadout loadout = FlatToModel.ToLoadout(Loadout, player.Team);

        ushort commandId = context.SpawnCommandQueue.AddBotSpawnCommand(
            player.Name,
            (int)player.Team,
            BotSkill.Custom,
            loadout
        );

        context.GameState.PlayerMapping.AddPendingSpawn(
            new SpawnTracker
            {
                CommandId = commandId,
                SpawnId = meta.SpawnId,
                DesiredPlayerIndex = meta.PlayerIndex,
                IsCustomBot = true,
                IsBot = true,
            }
        );

        context.SpawnCommandQueue.Flush();
    }
}
