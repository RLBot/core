namespace RLBotCS.Server.FlatbuffersMessage;

enum ServerAction
{
    Continue,
    Stop,
}

interface IServerMessage
{
    public ServerAction Execute(ServerContext context);
}
