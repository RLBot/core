namespace RLBotCS.Model;

public class AgentMetadata(uint index, uint team, string name, string agentId, int spawnId, uint matchId)
{
    public readonly uint Index = index;
    public readonly uint Team = team;
    public readonly string Name = name;
    public readonly string AgentId = agentId;
    public readonly int SpawnId = spawnId;
    public readonly uint MatchId = matchId;

    public int? ClientId { get; private set; } = null;
    public bool Ready { get; set; } = false;

    public bool IsScript => Team == Model.Team.Scripts;
    public bool HasClient => ClientId.HasValue;

    public void SetClient(int? clientId)
    {
        ClientId = clientId;
        Ready = false;
    }
}
