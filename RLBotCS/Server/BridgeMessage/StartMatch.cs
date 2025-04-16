using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Server.BridgeMessage;

/// <summary>
/// Begins the process of starting a match on the <see cref="BridgeHandler"/>.
/// This message should only be sent from the <see cref="FlatBuffersServer"/>,
/// not from <see cref="FlatBuffersSession"/>, as the <see cref="FlatBuffersServer"/>
/// needs to be reset too to avoid distributing old match configs. 
/// </summary>
record StartMatch(MatchConfigurationT MatchConfig) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.AgentMapping.SetAgents(MatchConfig);
        context.MatchStarter.StartMatch(MatchConfig, context.GetPlayerSpawner()); // May modify the match config
        
        // Notify clients about their agents indexes
        foreach (var infoRequest in context.WaitingAgentRequests)
        {
            infoRequest.HandleMessage(context);
        }
        
        context.WaitingAgentRequests.Clear();
    }
}
