using Bridge.Controller;
using Bridge.Models.Command;
using Bridge.Models.Message;
using Bridge.State;
using RLBot.Flat;
using RLBotCS.Conversion;

namespace RLBotCS.ManagerTools;

public readonly ref struct PlayerSpawner(
    ref GameState gameState,
    SpawnCommandQueue spawnCommandQueue
)
{
    private readonly ref GameState _gameState = ref gameState;

    public void SpawnBot(PlayerConfigurationT config, BotSkill skill, uint desiredIndex)
    {
        PlayerMetadata? alreadySpawnedPlayer = _gameState
            .PlayerMapping.GetKnownPlayers()
            .FirstOrDefault(kp => config.PlayerId == kp.PlayerId);
        if (alreadySpawnedPlayer != null)
            // We've already spawned this player, don't duplicate them.
            return;

        Loadout loadout = FlatToModel.ToLoadout(config.Loadout, config.Team);

        ushort commandId = spawnCommandQueue.AddBotSpawnCommand(
            config.Name,
            (int)config.Team,
            skill,
            loadout
        );

        _gameState.PlayerMapping.AddPendingSpawn(
            new SpawnTracker
            {
                CommandId = commandId,
                PlayerId = config.PlayerId,
                DesiredPlayerIndex = desiredIndex,
                IsCustomBot = skill == BotSkill.Custom,
                IsBot = true,
                AgentId = config.AgentId,
            }
        );
    }

    public void SpawnHuman(PlayerConfigurationT config, uint desiredIndex)
    {
        spawnCommandQueue.AddConsoleCommand("ChangeTeam " + config.Team);

        PlayerMetadata? alreadySpawnedPlayer = _gameState
            .PlayerMapping.GetKnownPlayers()
            .FirstOrDefault(kp => config.PlayerId == kp.PlayerId);
        if (alreadySpawnedPlayer != null)
        {
            _gameState.PlayerMapping.QueueIndexChange(
                alreadySpawnedPlayer.PlayerIndex,
                desiredIndex
            );
            return;
        }

        _gameState.PlayerMapping.AddPendingSpawn(
            new SpawnTracker
            {
                CommandId = 0, // Human spawning must use command id 0 for reasons in bridge
                PlayerId = config.PlayerId,
                DesiredPlayerIndex = desiredIndex,
                IsBot = false,
                IsCustomBot = false,
                AgentId = config.AgentId,
            }
        );
    }

    public void MakeHumanSpectate()
    {
        spawnCommandQueue.AddConsoleCommand("spectate");
    }

    public void DespawnPlayers(List<int> playerIds)
    {
        foreach (int playerId in playerIds)
        {
            PlayerMetadata? player = _gameState
                .PlayerMapping.GetKnownPlayers()
                .FirstOrDefault(p => p.PlayerId == playerId);

            if (player != null)
            {
                spawnCommandQueue.AddDespawnCommand(player.ActorId);
            }
        }
    }

    public string SpawnMap(MatchConfigurationT matchConfig)
    {
        string loadMapCommand = FlatToCommand.MakeOpenCommand(matchConfig);
        spawnCommandQueue.AddConsoleCommand(loadMapCommand);
        spawnCommandQueue.Flush();
        return loadMapCommand;
    }

    public void Flush()
    {
        spawnCommandQueue.Flush();
    }
}
