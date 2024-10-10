using Bridge.State;
using rlbot.flat;
using RLBotCS.ManagerTools;
using GoalInfo = Bridge.Packet.GoalInfo;

namespace RLBotCS.Server.FlatbuffersMessage;

internal record DistributeGameState(GameState GameState, GamePacketT? Packet) : IServerMessage
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

        context.FieldInfo.BoostPads.Sort(
            (a, b) =>
            {
                if (a.Location.Y != b.Location.Y)
                    return a.Location.Y.CompareTo(b.Location.Y);
                return a.Location.X.CompareTo(b.Location.X);
            }
        );

        // Distribute the field info to all waiting sessions
        foreach (var writer in context.FieldInfoWriters)
        {
            writer.TryWrite(new SessionMessage.FieldInfo(context.FieldInfo));
        }

        context.FieldInfoWriters.Clear();
    }

    private static void DistributeBallPrediction(ServerContext context, GamePacketT packet)
    {
        var firstBall = packet.Balls.FirstOrDefault();
        if (firstBall is null)
            return;

        (TouchT, uint)? lastTouch = null;

        foreach (var car in packet.Players)
        {
            if (car.LatestTouch is TouchT touch && touch.BallIndex == 0)
            {
                lastTouch = (touch, car.Team);
                break;
            }
        }

        BallPredictionT prediction = BallPredictor.Generate(
            context.PredictionMode,
            packet.GameInfo.SecondsElapsed,
            firstBall,
            lastTouch
        );

        foreach (var (writer, _, _) in context.Sessions.Values)
        {
            SessionMessage message = new SessionMessage.DistributeBallPrediction(prediction);
            writer.TryWrite(message);
        }
    }

    private static void DistributeState(ServerContext context, GamePacketT packet)
    {
        context.LastTickPacket = packet;
        foreach (var (writer, _, _) in context.Sessions.Values)
        {
            SessionMessage message = new SessionMessage.DistributeGameState(
                context.LastTickPacket
            );
            writer.TryWrite(message);
        }
    }

    public ServerAction Execute(ServerContext context)
    {
        UpdateFieldInfo(context, GameState);
        context.MatchStarter.MatchEnded = GameState.MatchEnded;

        if (Packet is GamePacketT packet)
        {
            DistributeBallPrediction(context, packet);
            DistributeState(context, packet);
        }

        return ServerAction.Continue;
    }
}
