namespace RLBotCS.Model;

public class Agent(uint index, uint team, string name, string agentId, uint matchId)
{
    public readonly uint Index = index;
    public readonly uint Team = team;
    public readonly string Name = name;
    public readonly string AgentId = agentId;
    public readonly uint MatchId = matchId;
    
    public int? ClientId { get; private set; } = null;
    public bool Ready { get; private set; } = false;
}
