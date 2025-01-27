using Bridge.Models.Command;
using Bridge.Models.Message;
using Bridge.State;
using rlbot.flat;
using RLBotCS.Conversion;

namespace RLBotCS.Server.BridgeMessage;

record SpawnBot(
    PlayerConfigurationT Config,
    BotSkill Skill,
    uint DesiredIndex,
    bool IsCustomBot
) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        PlayerMetadata? alreadySpawnedPlayer = context
            .GameState.PlayerMapping.GetKnownPlayers()
            .FirstOrDefault(kp => Config.SpawnId == kp.SpawnId);
        if (alreadySpawnedPlayer != null)
            // We've already spawned this player, don't duplicate them.
            return;

        Config.Loadout ??= new PlayerLoadoutT();
        Config.Loadout.LoadoutPaint ??= new LoadoutPaintT();
        Loadout loadout = FlatToModel.ToLoadout(Config.Loadout, Config.Team);

        context.QueuedMatchCommands = true;
        context.QueuingCommandsComplete = false;
        ushort commandId = context.MatchCommandSender.AddBotSpawnCommand(
            Config.Name,
            (int)Config.Team,
            Skill,
            loadout
        );

        context.GameState.PlayerMapping.AddPendingSpawn(
            new SpawnTracker
            {
                CommandId = commandId,
                SpawnId = Config.SpawnId,
                DesiredPlayerIndex = DesiredIndex,
                IsCustomBot = IsCustomBot,
                IsBot = true,
            }
        );
    }
}