using Bridge.State;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.ManagerTools;
using GoalInfo = Bridge.Packet.GoalInfo;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record DistributeGameState(GameState GameState, bool timeAdvanced) : IServerMessage
{
    private static void UpdateFieldInfo(ServerContext context, GameState gameState)
    {
        if (
            !context.ShouldUpdateFieldInfo
            || context.FieldInfo != null
            || gameState.BoostPads.Count == 0
            || gameState.Goals.Count == 0
        )
            return;

        context.FieldInfo = new FieldInfoT
        {
            BoostPads = new List<BoostPadT>(gameState.BoostPads.Count),
            Goals = new List<GoalInfoT>(gameState.Goals.Count)
        };

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

        foreach (var boostPad in gameState.BoostPads.Values)
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
            writer.TryWrite(new SessionMessage.FieldInfo(context.FieldInfo));
        }

        context.FieldInfoWriters.Clear();
    }

    private static void DistributeBallPrediction(ServerContext context, GameState gameState)
    {
        var firstBall = gameState.Balls.Values.FirstOrDefault();
        if (firstBall is null)
            return;

        BallPredictionT prediction = BallPredictor.Generate(
            context.PredictionMode,
            gameState.SecondsElapsed,
            firstBall
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

        var gameTickPacket = gameState.ToFlatBuffers();
        foreach (var (writer, _) in context.Sessions.Values)
        {
            SessionMessage message = new SessionMessage.DistributeGameState(gameTickPacket);
            writer.TryWrite(message);
        }
    }

    public ServerAction Execute(ServerContext context)
    {
        UpdateFieldInfo(context, GameState);

        if (timeAdvanced)
        {
            DistributeBallPrediction(context, GameState);
            DistributeState(context, GameState);
        }

        return ServerAction.Continue;
    }
}
