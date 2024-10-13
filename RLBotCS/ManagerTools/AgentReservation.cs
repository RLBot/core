using rlbot.flat;

namespace RLBotCS.ManagerTools;

public struct PlayerIdPair
{
    public uint Index;
    public int SpawnId;
}

public class AgentReservation
{
    private class PlayerMetadata
    {
        public uint Index;
        public int SpawnId;
        public uint Team;
        public string AgentId = "";
        public bool IsReserved;
    }

    private readonly List<PlayerMetadata> _knownPlayers = new();

    public void SetPlayers(MatchSettingsT matchSettings)
    {
        _knownPlayers.Clear();

        uint indexOffset = 0;
        for (int i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
        {
            var playerConfig = matchSettings.PlayerConfigurations[i];

            if (playerConfig.Variety.Type != PlayerClass.RLBot)
            {
                if (playerConfig.Variety.Type == PlayerClass.Human)
                    indexOffset++;

                continue;
            }

            uint index = (uint)i - indexOffset;
            _knownPlayers.Add(
                (
                    new PlayerMetadata
                    {
                        Index = index,
                        SpawnId = playerConfig.SpawnId,
                        Team = playerConfig.Team,
                        AgentId = playerConfig.AgentId,
                    }
                )
            );
        }
    }

    public (PlayerIdPair, uint)? ReservePlayer(string agentId)
    {
        PlayerMetadata? player = _knownPlayers.FirstOrDefault(
            playerMetadata => !playerMetadata.IsReserved && playerMetadata.AgentId == agentId
        );
        if (player != null)
        {
            player.IsReserved = true;

            return (
                new PlayerIdPair { Index = player.Index, SpawnId = player.SpawnId },
                player.Team
            );
        }

        return null;
    }

    public (List<PlayerIdPair>, uint)? ReservePlayers(string agentId)
    {
        // find the first player in the group
        if (ReservePlayer(agentId) is (PlayerIdPair, uint) initalPlayer)
        {
            var playerIdPair = initalPlayer.Item1;
            var team = initalPlayer.Item2;

            List<PlayerIdPair> players = new() { playerIdPair };

            // find other players in the same group & team

            var otherPlayers = _knownPlayers.Where(
                playerMetadata =>
                    !playerMetadata.IsReserved
                    && playerMetadata.AgentId == agentId
                    && playerMetadata.Team == team
            );

            foreach (var playerMetadata in otherPlayers)
            {
                playerMetadata.IsReserved = true;
                players.Add(
                    new PlayerIdPair
                    {
                        Index = playerMetadata.Index,
                        SpawnId = playerMetadata.SpawnId
                    }
                );
            }

            return (players, team);
        }

        return null;
    }
}
