using System.Drawing;
using Bridge.Models.Command;
using Bridge.Models.Control;
using Bridge.Models.Phys;

namespace Bridge.Conversion
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

        internal static Vector3 ToVectorFromT(rlbot.flat.Vector3T vec)
        {
            return new Vector3()
            {
                x = vec.X,
                y = vec.Y,
                z = vec.Z
            };
        }

        internal static Color ToColor(rlbot.flat.ColorT c)
        {
            return Color.FromArgb(c.A, c.R, c.G, c.B);
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

        internal static Vector3 DesiredToVector(rlbot.flat.Vector3PartialT? partVec, Vector3 defaultVec)
        {
            if (partVec is rlbot.flat.Vector3PartialT vec)
            {
                return new Vector3()
                {
                    x = vec.X?.Val ?? defaultVec.x,
                    y = vec.Y?.Val ?? defaultVec.y,
                    z = vec.Z?.Val ?? defaultVec.z
                };
            }
            else
            {
                return defaultVec;
            }
        }

        internal static Rotator DesiredToRotator(rlbot.flat.RotatorPartialT? partRot, Rotator defaultRot)
        {
            if (partRot is rlbot.flat.RotatorPartialT rot)
            {
                return new Rotator()
                {
                    pitch = rot.Pitch?.Val ?? defaultRot.pitch,
                    yaw = rot.Yaw?.Val ?? defaultRot.yaw,
                    roll = rot.Roll?.Val ?? defaultRot.roll
                };
            }
            else
            {
                return defaultRot;
            }
        }

        internal static Physics DesiredToPhysics(rlbot.flat.DesiredPhysicsT p, Physics defaultP)
        {
            return new Physics()
            {
                location = DesiredToVector(p.Location, defaultP.location),
                rotation = DesiredToRotator(p.Rotation, defaultP.rotation),
                velocity = DesiredToVector(p.Velocity, defaultP.velocity),
                angularVelocity = DesiredToVector(p.AngularVelocity, defaultP.angularVelocity),
            };
        }
    }
}
