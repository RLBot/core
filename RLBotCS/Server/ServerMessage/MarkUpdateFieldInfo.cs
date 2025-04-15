namespace RLBotCS.Server.ServerMessage;

record MarkUpdateFieldInfo : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.FieldInfo = null;
        context.ShouldUpdateFieldInfo = true;

        return ServerAction.Continue;
    }
}
