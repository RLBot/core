namespace RLBotCS.Server.FlatbuffersMessage;

internal enum ServerAction
{
    Continue,
    Stop
}

internal interface IServerMessage
{
    public ServerAction Execute(ServerContext context);
}