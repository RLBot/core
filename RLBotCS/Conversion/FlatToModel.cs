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
                Throttle = state.Throttle,
                Steer = state.Steer,
                Pitch = state.Pitch,
                Yaw = state.Yaw,
                Roll = state.Roll,
                Jump = state.Jump,
                Boost = state.Boost,
                Handbrake = state.Handbrake,
                UseItem = state.UseItem,
                DodgeForward = dodgeForward,
                DodgeStrafe = dodgeStrafe,
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
                    CarPaintId = (byte)lp.CarPaintId,
                    DecalPaintId = (byte)lp.DecalPaintId,
                    WheelsPaintId = (byte)lp.WheelsPaintId,
                    BoostPaintId = (byte)lp.BoostPaintId,
                    AntennaPaintId = (byte)lp.AntennaPaintId,
                    HatPaintId = (byte)lp.HatPaintId,
                    TrailsPaintId = (byte)lp.TrailsPaintId,
                    GoalExplosionPaintId = (byte)lp.GoalExplosionPaintId
                };

            return new Loadout()
            {
                CarId = (ushort)l.CarId,
                AntennaId = (ushort)l.AntennaId,
                BoostId = (ushort)l.BoostId,
                EngineAudioId = (ushort)l.EngineAudioId,
                CustomFinishId = (ushort)l.CustomFinishId,
                DecalId = (ushort)l.DecalId,
                GoalExplosionId = (ushort)l.GoalExplosionId,
                HatId = (ushort)l.HatId,
                PaintFinishId = (ushort)l.PaintFinishId,
                TrailsId = (ushort)l.TrailsId,
                WheelsId = (ushort)l.WheelsId,
                LoadoutPaint = loadoutPaint,
                PrimaryColorLookup = primaryColor,
                SecondaryColorLookup = secondaryColor
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
