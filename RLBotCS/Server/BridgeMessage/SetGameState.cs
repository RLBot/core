using rlbot.flat;
using RLBotCS.Conversion;

namespace RLBotCS.Server.BridgeMessage;

record SetGameState(DesiredGameStateT GameState) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        foreach (var command in GameState.ConsoleCommands)
            context.MatchCommandQueue.AddConsoleCommand(command.Command);

        if (GameState.MatchInfo is { } matchInfo)
        {
            if (matchInfo.WorldGravityZ is { } gravity)
            {
                context.MatchCommandQueue.AddConsoleCommand(
                    FlatToCommand.MakeGravityCommand(gravity.Val)
                );
                context.GameState.WorldGravityZ = gravity.Val;
            }

            if (matchInfo.GameSpeed is { } speed)
                context.MatchCommandQueue.AddConsoleCommand(
                    FlatToCommand.MakeGameSpeedCommand(speed.Val)
                );
        }

        for (int i = 0; i < GameState.BallStates.Count; i++)
        {
            var ball = GameState.BallStates[i];
            var id = context.GameState.GetBallActorIdFromIndex((uint)i);

            if (id == null)
                continue;

            if (ball.Physics is { } physics)
            {
                var currentPhysics = context.GameState.Balls[(ushort)id].Physics;
                var fullState = FlatToModel.DesiredToPhysics(physics, currentPhysics);

                context.MatchCommandQueue.AddSetPhysicsCommand((ushort)id, fullState);
            }
        }

        for (int i = 0; i < GameState.CarStates.Count; i++)
        {
            var car = GameState.CarStates[i];
            var id = context.GameState.PlayerMapping.ActorIdFromPlayerIndex((uint)i);

            if (id == null)
                continue;

            if (car.Physics is { } physics)
            {
                var currentPhysics = context.GameState.GameCars[(uint)i].Physics;
                var fullState = FlatToModel.DesiredToPhysics(physics, currentPhysics);

                context.MatchCommandQueue.AddSetPhysicsCommand((ushort)id, fullState);
            }

            if (car.BoostAmount is { } boostAmount)
            {
                context.MatchCommandQueue.AddSetBoostCommand((ushort)id, (int)boostAmount.Val);
            }
        }

        context.MatchCommandQueue.Flush();
    }
}
