namespace RLBotCS.Server.ServerMessage;

enum ServerAction
{
    Continue,
    Stop,
}

/// <summary>
/// A message sent to <see cref="FlatBuffersServer"/>.
/// </summary>
interface IServerMessage
{
    public ServerAction Execute(ServerContext context);
}
