using Bridge.State;
using RLBot.Flat;
using GoalInfo = Bridge.Packet.GoalInfo;

namespace RLBotCS.Server.ServerMessage;

record DistributeFieldInfo(GameState GameState) : IServerMessage
{
    public ServerAction Execute(ServerContext context)
    {
        context.FieldInfo = new FieldInfoT
        {
            BoostPads = new List<BoostPadT>(GameState.BoostPads.Count),
            Goals = new List<GoalInfoT>(GameState.Goals.Count),
        };

        foreach (GoalInfo goal in GameState.Goals)
        {
            context.FieldInfo.Goals.Add(
                new GoalInfoT
                {
                    TeamNum = goal.Team,
                    Location = new Vector3T
                    {
                        X = goal.Location.X,
                        Y = goal.Location.Y,
                        Z = goal.Location.Z,
                    },
                    Direction = new Vector3T
                    {
                        X = goal.Direction.X,
                        Y = goal.Direction.Y,
                        Z = goal.Direction.Z,
                    },
                    Width = goal.Width,
                    Height = goal.Height,
                }
            );
        }

        foreach (var boostPad in GameState.BoostPads.Values)
        {
            context.FieldInfo.BoostPads.Add(
                new BoostPadT
                {
                    Location = new Vector3T
                    {
                        X = boostPad.SpawnPosition.X,
                        Y = boostPad.SpawnPosition.Y,
                        Z = boostPad.SpawnPosition.Z,
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
        foreach (var writer in context.WaitingFieldInfoRequests)
        {
            writer.TryWrite(new SessionMessage.FieldInfo(context.FieldInfo));
        }

        context.WaitingFieldInfoRequests.Clear();

        return ServerAction.Continue;
    }
}
