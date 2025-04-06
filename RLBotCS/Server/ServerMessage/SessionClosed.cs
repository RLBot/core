using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.ServerMessage;

record SessionClosed(int ClientId, bool decrReadies) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.Bridge.TryWrite(new RemoveClientRenders(ClientId));
        context.Sessions.Remove(ClientId);
        if (decrReadies)
        {
            context.MatchStarter.DecrementConnectionReady();
        }

        return ServerAction.Continue;
    }
}
