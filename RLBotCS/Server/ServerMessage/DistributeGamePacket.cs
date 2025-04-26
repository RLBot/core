using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Server.ServerMessage;

record DistributeGamePacket(GamePacketT? Packet) : IServerMessage
{
    private static void DistributeBallPrediction(ServerContext context, GamePacketT packet)
    {
        var firstBall = packet.Balls.FirstOrDefault();
        if (firstBall is null)
            return;

        (TouchT, uint)? lastTouch = null;

        foreach (var car in packet.Players)
        {
            if (car.LatestTouch is { } touch && touch.BallIndex == 0)
            {
                lastTouch = (touch, car.Team);
                break;
            }
        }

        BallPredictionT prediction = BallPredictor.Generate(
            packet.MatchInfo.SecondsElapsed,
            firstBall,
            lastTouch,
            packet.MatchInfo.WorldGravityZ
        );

        foreach (var (writer, _) in context.Sessions.Values)
        {
            SessionMessage message = new SessionMessage.DistributeBallPrediction(prediction);
            writer.TryWrite(message);
        }
    }

    private static void DistributeState(ServerContext context, GamePacketT packet)
    {
        context.LastTickPacket = packet;
        foreach (var (writer, _) in context.Sessions.Values)
        {
            SessionMessage message = new SessionMessage.DistributeGameState(
                context.LastTickPacket
            );
            writer.TryWrite(message);
        }
    }

    public ServerAction Execute(ServerContext context)
    {
        if (Packet is { } packet)
        {
            DistributeBallPrediction(context, packet);
            DistributeState(context, packet);
        }

        return ServerAction.Continue;
    }
}
