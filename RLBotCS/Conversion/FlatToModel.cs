using RLBotModels.Command;
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

        static internal Loadout ToLoadout(rlbot.flat.PlayerLoadoutT l, int team)
        {
            System.Drawing.Color primaryColor;
            if (l.PrimaryColorLookup is rlbot.flat.ColorT p)
            {
                primaryColor = System.Drawing.Color.FromArgb(p.A, p.R, p.G, p.B);
            }
            else
            {
                primaryColor = ColorSwatches.GetPrimary(l.TeamColorId, team);
            }

            System.Drawing.Color secondaryColor;
            if (l.SecondaryColorLookup is rlbot.flat.ColorT s)
            {
                secondaryColor = System.Drawing.Color.FromArgb(s.A, s.R, s.G, s.B);
            }
            else
            {
                secondaryColor = ColorSwatches.GetSecondary(l.CustomColorId);
            }

            var lp = l.LoadoutPaint;

            LoadoutPaint loadoutPaint = new() {
                carPaintId = (byte)lp.CarPaintId,
                decalPaintId = (byte)lp.DecalPaintId,
                wheelsPaintId = (byte)lp.WheelsPaintId,
                boostPaintId = (byte)lp.BoostPaintId,
                antennaPaintId = (byte)lp.AntennaPaintId,
                hatPaintId = (byte)lp.HatPaintId,
                trailsPaintId = (byte)lp.TrailsPaintId,
                goalExplosionPaintId = (byte)lp.GoalExplosionPaintId
            };

            return new Loadout()
            {
                carId = (ushort)l.CarId,
                antennaId = (ushort)l.AntennaId,
                boostId = (ushort)l.BoostId,
                engineAudioId = (ushort)l.EngineAudioId,
                customFinishId = (ushort)l.CustomFinishId,
                decalId = (ushort)l.DecalId,
                goalExplosionId = (ushort)l.GoalExplosionId,
                hatId = (ushort)l.HatId,
                paintFinishId = (ushort)l.PaintFinishId,
                trailsId = (ushort)l.TrailsId,
                wheelsId = (ushort)l.WheelsId,
                loadoutPaint = loadoutPaint,
                primaryColorLookup = primaryColor,
                secondaryColorLookup = secondaryColor
            };
        }

        static internal Vector3 DesiredToVector(rlbot.flat.Vector3PartialT vec)
        {
            return new Vector3() {
                x = vec.X?.Val ?? 0,
                y = vec.Y?.Val ?? 0,
                z = vec.Z?.Val ?? 0
            };
        }

        static internal Rotator DesiredToRotator(rlbot.flat.RotatorPartialT rot)
        {
            return new Rotator() {
                pitch = rot.Pitch?.Val ?? 0,
                yaw = rot.Yaw?.Val ?? 0,
                roll = rot.Roll?.Val ?? 0
            };
        }

        static internal Physics DesiredToPhysics(rlbot.flat.DesiredPhysicsT p)
        {
            return new Physics()
            {
                location = DesiredToVector(p.Location),
                rotation = DesiredToRotator(p.Rotation),
                velocity = DesiredToVector(p.Velocity),
                angularVelocity = DesiredToVector(p.AngularVelocity),
            };
        }

    }
}
