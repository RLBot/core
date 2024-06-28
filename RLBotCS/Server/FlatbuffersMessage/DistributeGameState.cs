using Bridge.Models.Message;
using Bridge.State;
using rlbot.flat;
using RLBotCS.ManagerTools;
using GoalInfo = Bridge.Packet.GoalInfo;

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
                    Location = new Vector3T
                    {
                        X = goal.Location.X,
                        Y = goal.Location.Y,
                        Z = goal.Location.Z
                    },
                    Direction = new Vector3T
                    {
                        X = goal.Direction.X,
                        Y = goal.Direction.Y,
                        Z = goal.Direction.Z
                    },
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
                        X = boostPad.SpawnPosition.X,
                        Y = boostPad.SpawnPosition.Y,
                        Z = boostPad.SpawnPosition.Z
                    },
                    IsFullBoost = boostPad.IsFullBoost,
                }
            );
        }

        // Distribute the field info to all waiting sessions
        foreach (var writer in context.FieldInfoWriters)
        {
            writer.TryWrite(context.FieldInfo);
            writer.TryComplete();
        }

        context.FieldInfoWriters.Clear();
    }

    private static void DistributeBallPrediction(ServerContext context, GameState gameState)
    {
        BallPredictionT prediction = BallPredictor.Generate(
            context.PredictionMode,
            gameState.SecondsElapsed,
            gameState.Ball
        );

        foreach (var (writer, _) in context.Sessions.Values)
        {
            SessionMessage message = new SessionMessage.DistributeBallPrediction(prediction);
            writer.TryWrite(message);
        }
    }

    private static void DistributeState(ServerContext context, GameState gameState)
    {
        context.MatchStarter.MatchEnded = gameState.MatchEnded;

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
