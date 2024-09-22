using rlbot.flat;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record SpawnLoadout(PlayerLoadoutT Loadout, int SpawnId) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.MatchStarter.AddLoadout(Loadout, SpawnId);

        return ServerAction.Continue;
    }
}
