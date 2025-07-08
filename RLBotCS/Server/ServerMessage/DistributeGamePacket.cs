using RLBot.Flat;
using RLBotCS.ManagerTools;

namespace RLBotCS.Server.ServerMessage;

record DistributeGamePacket(GamePacketT Packet) : IServerMessage
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
        context.DistributeMessage(new SessionMessage.DistributeBallPrediction(prediction));
    }

    private static void DistributeState(ServerContext context, GamePacketT packet)
    {
        context.LastTickPacket = packet;
        context.DistributeMessage(
            new SessionMessage.DistributeGameState(context.LastTickPacket)
        );
    }

    public ServerAction Execute(ServerContext context)
    {
        DistributeBallPrediction(context, Packet);
        DistributeState(context, Packet);

        return ServerAction.Continue;
    }
}
