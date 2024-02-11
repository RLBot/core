using RLBotSecret.Models.Command;
using RLBotSecret.Models.Control;
using RLBotSecret.Models.Phys;

namespace RLBotSecret.Conversion
{
    internal class FlatToModel
    {
        internal static CarInput ToCarInput(rlbot.flat.ControllerState state)
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

        internal static Vector3 ToVector(rlbot.flat.Vector3 vec)
        {
            return new Vector3()
            {
                x = vec.X,
                y = vec.Y,
                z = vec.Z
            };
        }

        internal static Rotator ToRotator(rlbot.flat.Rotator r)
        {
            return new Rotator()
            {
                pitch = r.Pitch,
                yaw = r.Yaw,
                roll = r.Roll
            };
        }

        internal static Loadout ToLoadout(rlbot.flat.PlayerLoadoutT l, uint team)
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

            LoadoutPaint loadoutPaint =
                new()
                {
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

        internal static Vector3 DesiredToVector(rlbot.flat.Vector3PartialT? part_vec, Vector3 default_vec)
        {
            if (part_vec is rlbot.flat.Vector3PartialT vec)
            {
                return new Vector3()
                {
                    x = vec.X?.Val ?? default_vec.x,
                    y = vec.Y?.Val ?? default_vec.y,
                    z = vec.Z?.Val ?? default_vec.z
                };
            }
            else
            {
                return default_vec;
            }
        }

        internal static Rotator DesiredToRotator(rlbot.flat.RotatorPartialT? part_rot, Rotator default_rot)
        {
            if (part_rot is rlbot.flat.RotatorPartialT rot)
            {
                return new Rotator()
                {
                    pitch = rot.Pitch?.Val ?? default_rot.pitch,
                    yaw = rot.Yaw?.Val ?? default_rot.yaw,
                    roll = rot.Roll?.Val ?? default_rot.roll
                };
            }
            else
            {
                return default_rot;
            }
        }

        internal static Physics DesiredToPhysics(rlbot.flat.DesiredPhysicsT p, Physics default_p)
        {
            return new Physics()
            {
                location = DesiredToVector(p.Location, default_p.location),
                rotation = DesiredToRotator(p.Rotation, default_p.rotation),
                velocity = DesiredToVector(p.Velocity, default_p.velocity),
                angularVelocity = DesiredToVector(p.AngularVelocity, default_p.angularVelocity),
            };
        }
    }
}
