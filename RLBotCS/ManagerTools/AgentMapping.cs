using Bridge.State;
using rlbot.flat;
using RLBotCS.Model;

namespace RLBotCS.ManagerTools;

public struct PlayerIdPair
{
    public uint Index;
    public int SpawnId;
}

public class AgentMapping
{
    private readonly List<AgentMetadata> _agents = new();

    public void SetAgents(MatchConfigurationT matchConfig)
    {
        _agents.Clear();

        // Bots
        uint humans = 0;
        for (int i = 0; i < matchConfig.PlayerConfigurations.Count; i++)
        {
            var playerConfig = matchConfig.PlayerConfigurations[i];

            if (playerConfig.Variety.Type != PlayerClass.CustomBot)
            {
                if (playerConfig.Variety.Type == PlayerClass.Human)
                    humans++;

                continue;
            }

            uint index = (uint)i - humans;
            _agents.Add(
                new AgentMetadata(
                    index, playerConfig.Team, playerConfig.Name, playerConfig.AgentId, playerConfig.SpawnId, 0
                )
            );
        }

        // Scripts
        for (int i = 0; i < matchConfig.ScriptConfigurations.Count; i++)
        {
            var scriptConfig = matchConfig.ScriptConfigurations[i];
            _agents.Add(
                new AgentMetadata(
                    (uint)i, Team.Scripts, scriptConfig.Name, scriptConfig.AgentId, scriptConfig.SpawnId, 0
                )
            );
        }
    }

    public (PlayerIdPair, uint)? ReserveAgent(int clientId, string agentId)
    {
        AgentMetadata? player = _agents.FirstOrDefault(playerMetadata =>
            !playerMetadata.HasClient && playerMetadata.AgentId == agentId
        );
        if (player != null)
        {
            player.SetClient(clientId);

            return (
                new PlayerIdPair { Index = player.Index, SpawnId = player.SpawnId },
                player.Team
            );
        }

        return null;
    }

    public (List<PlayerIdPair>, uint)? ReserveAgents(int clientId, string agentId)
    {
        // find the first player in the group
        if (ReserveAgent(clientId, agentId) is var (playerIdPair, team))
        {
            List<PlayerIdPair> players = new() { playerIdPair };

            // find other players in the same group & team

            var otherPlayers = _agents.Where(playerMetadata =>
                !playerMetadata.HasClient
                && playerMetadata.AgentId == agentId
                && playerMetadata.Team == team
            );

            foreach (var playerMetadata in otherPlayers)
            {
                playerMetadata.SetClient(clientId);
                players.Add(
                    new PlayerIdPair
                    {
                        Index = playerMetadata.Index,
                        SpawnId = playerMetadata.SpawnId,
                    }
                );
            }

            return (players, team);
        }

        return null;
    }

    public void UnreserveAgents(int clientId)
    {
        foreach (var agent in _agents)
        {
            if (agent.ClientId == clientId)
            {
                agent.SetClient(null);
            }
        }
    }

    public void ReadyAgents(int clientId)
    {
        foreach (var agent in _agents)
        {
            if (agent.ClientId == clientId)
            {
                agent.Ready = true;
            }
        }
    }
    
    public bool AllReady() => _agents.All(a => a.Ready);

    /// <summary>Returns how many agents are ready and how many there are in total.</summary>
    public (int, int) GetReadyStatus()
    {
        var ready = _agents.Count(a => a.Ready);
        var expected = _agents.Count;
        return (ready, expected);
    }
}
