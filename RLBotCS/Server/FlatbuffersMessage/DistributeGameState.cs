using rlbot.flat;
using RLBotSecret.Models.Message;
using RLBotSecret.Packet;
using RLBotSecret.State;
using GoalInfo = RLBotSecret.Packet.GoalInfo;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record DistributeGameState(GameState GameState) : IServerMessage
{
    private static void UpdateFieldInfo(ServerContext context, GameState gameState)
    {
        if (!context.ShouldUpdateFieldInfo)
            return;

        if (context.FieldInfo == null)
            context.FieldInfo = new FieldInfoT { Goals = [], BoostPads = [] };
        else
        {
            context.FieldInfo.Goals.Clear();
            context.FieldInfo.BoostPads.Clear();
        }

        foreach (GoalInfo goal in gameState.Goals)
        {
            context.FieldInfo.Goals.Add(
                new GoalInfoT
                {
                    TeamNum = goal.Team,
                    Location =
                        new Vector3T { X = goal.Location.x, Y = goal.Location.y, Z = goal.Location.z },
                    Direction = new Vector3T { X = goal.Direction.x, Y = goal.Direction.y, Z = goal.Direction.z },
                    Width = goal.Width,
                    Height = goal.Height,
                }
            );
        }

        foreach (BoostPadSpawn boostPad in gameState.BoostPads)
        {
            context.FieldInfo.BoostPads.Add(
                new BoostPadT
                {
                    Location = new Vector3T
                    {
                        X = boostPad.SpawnPosition.x,
                        Y = boostPad.SpawnPosition.y,
                        Z = boostPad.SpawnPosition.z
                    },
                    IsFullBoost = boostPad.IsFullBoost,
                }
            );
        }

        // distribute the field info to all waiting sessions
        foreach (var writer in context.FieldInfoWriters)
        {
            writer.TryWrite(context.FieldInfo);
            writer.TryComplete();
        }

        context.FieldInfoWriters.Clear();
    }

    private void DistributeBallPrediction(ServerContext context, GameState gameState)
    {
        BallPredictionT prediction = context.BallPredictor.Generate(gameState.SecondsElapsed, gameState.Ball);

        foreach (var (writer, _) in context.Sessions.Values)
        {
            SessionMessage message = new SessionMessage.DistributeBallPrediction(prediction);
            writer.TryWrite(message);
        }
    }

    private static void DistributeState(ServerContext context, GameState gameState)
    {
        context.MatchStarter.matchEnded = gameState.MatchEnded;

        foreach (var (writer, _) in context.Sessions.Values)
        {
            SessionMessage message = new SessionMessage.DistributeGameState(gameState);
            writer.TryWrite(message);
        }
    }

    public ServerAction Execute(ServerContext context)
    {
        UpdateFieldInfo(context, GameState);
        DistributeBallPrediction(context, GameState);
        DistributeState(context, GameState);

        return ServerAction.Continue;
    }
}