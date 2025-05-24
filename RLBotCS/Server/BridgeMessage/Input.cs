using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBotCS.Conversion;
using PlayerInput = Bridge.Models.Control.PlayerInput;

namespace RLBotCS.Server.BridgeMessage;

record Input(PlayerInputT PlayerInput) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        ushort? actorId = context.GameState.PlayerMapping.ActorIdFromPlayerIndex(
            PlayerInput.PlayerIndex
        );

        if (actorId is { } actorIdValue)
        {
            PlayerInput playerInput = new()
            {
                ActorId = actorIdValue,
                CarInput = FlatToModel.ToCarInput(PlayerInput.ControllerState),
            };
            context.PlayerInputSender.SendPlayerInput(playerInput);
        }
        else if (
            !context.GameState.PlayerMapping.IsPlayerIndexPending(PlayerInput.PlayerIndex)
        )
            context.Logger.LogError(
                $"Got input from unknown player index {PlayerInput.PlayerIndex}"
            );
    }
}
