using Bridge.Controller;
using Bridge.Models.Command;
using Bridge.Models.Message;
using Bridge.State;
using rlbot.flat;
using RLBotCS.Conversion;

namespace RLBotCS.ManagerTools;

public readonly ref struct PlayerSpawner(ref GameState gameState, MatchCommandSender matchCommandSender)
{
    private readonly ref GameState _gameState = ref gameState;

    public void SpawnBot(PlayerConfigurationT config, BotSkill skill, uint desiredIndex)
    {
        PlayerMetadata? alreadySpawnedPlayer = _gameState.PlayerMapping.GetKnownPlayers()
            .FirstOrDefault(kp => config.SpawnId == kp.SpawnId);
        if (alreadySpawnedPlayer != null)
            // We've already spawned this player, don't duplicate them.
            return;

        Loadout loadout = FlatToModel.ToLoadout(config.Loadout, config.Team);

        ushort commandId = matchCommandSender.AddBotSpawnCommand(
            config.Name,
            (int)config.Team,
            skill,
            loadout
        );

        _gameState.PlayerMapping.AddPendingSpawn(
            new SpawnTracker
            {
                CommandId = commandId,
                SpawnId = config.SpawnId,
                DesiredPlayerIndex = desiredIndex,
                IsCustomBot = skill == BotSkill.Custom,
                IsBot = true,
            }
        );
    }

    public void SpawnHuman(PlayerConfigurationT config, uint desiredIndex)
    {
        matchCommandSender.AddConsoleCommand("ChangeTeam " + config.Team);

        PlayerMetadata? alreadySpawnedPlayer = _gameState.PlayerMapping.GetKnownPlayers()
            .FirstOrDefault(kp => config.SpawnId == kp.SpawnId);
        if (alreadySpawnedPlayer != null)
        {
            alreadySpawnedPlayer.PlayerIndex = desiredIndex;
            return;
        }

        _gameState.PlayerMapping.AddPendingSpawn(
            new SpawnTracker
            {
                CommandId = 0,
                SpawnId = config.SpawnId,
                DesiredPlayerIndex = desiredIndex,
                IsBot = false,
                IsCustomBot = false,
            }
        );
    }

    public void MakeHumanSpectate()
    {
        matchCommandSender.AddConsoleCommand("spectate");
    }

    public void DespawnPlayers(List<int> spawnIds)
    {
        foreach (int spawnId in spawnIds)
        {
            PlayerMetadata? player = _gameState.PlayerMapping.GetKnownPlayers()
                .FirstOrDefault(p => p.SpawnId == spawnId);

            if (player != null)
            {
                matchCommandSender.AddDespawnCommand(player.ActorId);
            }
        }
    }

    public string SpawnMap(MatchConfigurationT matchConfig)
    {
        string loadMapCommand = FlatToCommand.MakeOpenCommand(matchConfig);
        matchCommandSender.AddConsoleCommand(loadMapCommand);
        matchCommandSender.Send();
        return loadMapCommand;
    }

    public void Flush()
    {
        matchCommandSender.Send();
    }
}
