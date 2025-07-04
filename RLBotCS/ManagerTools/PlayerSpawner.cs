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
        string botName;
        string agentId;
        PlayerLoadoutT configLoadout;
        switch (config.Variety.Value)
        {
            case PsyonixBotT bot:
                botName = bot.Name;
                agentId = $"psyonix/{bot.BotSkill}";
                configLoadout = bot.Loadout;
                break;
            case CustomBotT bot:
                botName = bot.Name;
                agentId = bot.AgentId;
                configLoadout = bot.Loadout;
                break;
            default:
                return;
        }

        PlayerMetadata? alreadySpawnedPlayer = _gameState
            .PlayerMapping.GetKnownPlayers()
            .FirstOrDefault(kp => config.PlayerId == kp.PlayerId);
        if (alreadySpawnedPlayer != null)
            // We've already spawned this player, don't duplicate them.
            return;

        Loadout loadout = FlatToModel.ToLoadout(configLoadout, config.Team);

        ushort commandId = spawnCommandQueue.AddBotSpawnCommand(
            botName,
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
                AgentId = agentId,
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
                AgentId = "human",
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

    public (string, CustomMap?) SpawnMap(MatchConfigurationT matchConfig)
    {
        (string loadMapCommand, CustomMap? customMap) = FlatToCommand.MakeOpenCommand(
            matchConfig
        );
        spawnCommandQueue.AddConsoleCommand(loadMapCommand);
        spawnCommandQueue.Flush();
        return (loadMapCommand, customMap);
    }

    public void Flush()
    {
        spawnCommandQueue.Flush();
    }
}
