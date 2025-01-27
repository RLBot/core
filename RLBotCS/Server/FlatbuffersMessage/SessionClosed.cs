using RLBotCS.Server.BridgeMessage;

namespace RLBotCS.Server.FlatbuffersMessage;

record SessionClosed(int ClientId) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.Bridge.TryWrite(new RemoveClientRenders(ClientId));
        context.Sessions.Remove(ClientId);

        return ServerAction.Continue;
    }
}
