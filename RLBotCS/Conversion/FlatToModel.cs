using RLBotModels.Control;
using RLBotModels.Phys;

namespace RLBotSecret.Conversion
{
    internal class FlatToModel
    {
        static internal CarInput ToCarInput(rlbot.flat.ControllerState state)
        {
            float dodgeForward = -state.Pitch;

            // Setting strafe = yaw allows us to use the "stall" mechanic as expected.
            float dodgeStrafe = state.Yaw;

            // TODO: consider clamping all the values between -1 and 1. Old RLBot did that.

            return new CarInput()
            {
                throttle = state.Throttle,
                steer = state.Steer,
                pitch = state.Pitch,
                yaw = state.Yaw,
                roll = state.Roll,
                jump = state.Jump,
                boost = state.Boost,
                handbrake = state.Handbrake,
                useItem = state.UseItem,
                dodgeForward = dodgeForward,
                dodgeStrafe = dodgeStrafe,
            };
        }

        static internal Vector3 ToVector(rlbot.flat.Vector3 vec)
        {
            return new Vector3() { x = vec.X, y = vec.Y, z = vec.Z };
        }

        static internal Rotator ToRotator(rlbot.flat.Rotator r)
        {
            return new Rotator() { pitch = r.Pitch, yaw = r.Yaw, roll = r.Roll };
        }
    }
}
