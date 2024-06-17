using rlbot.flat;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record StartMatch(MatchSettingsT MatchSettings) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.MatchStarter.StartMatch(MatchSettings);

        // Distribute the match settings to all waiting sessions
        foreach (var writer in context.MatchSettingsWriters)
        {
            writer.TryWrite(MatchSettings);
            writer.TryComplete();
        }

        context.MatchSettingsWriters.Clear();

        return ServerAction.Continue;
    }
}