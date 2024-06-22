using System.Drawing;
using Bridge.Models.Command;
using Bridge.Models.Control;
using Bridge.Models.Phys;

namespace RLBotCS.Conversion;

internal static class FlatToModel
{
    internal static CarInput ToCarInput(rlbot.flat.ControllerState state)
    {
        float dodgeForward = -state.Pitch;

        // Setting strafe = yaw allows us to use the "stall" mechanic as expected.
        float dodgeStrafe = state.Yaw;

        // TODO: consider clamping all the values between -1 and 1. Old RLBot did that.

        return new CarInput
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

    internal static Vector3 ToVector(rlbot.flat.Vector3 vec) =>
        new()
        {
            x = vec.X,
            y = vec.Y,
            z = vec.Z
        };

    internal static Vector3 ToVectorFromT(rlbot.flat.Vector3T vec)
    {
        return new Vector3
        {
            x = vec.X,
            y = vec.Y,
            z = vec.Z
        };
    }

    internal static Color ToColor(rlbot.flat.ColorT c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    internal static Rotator ToRotator(rlbot.flat.Rotator r)
    {
        return new Rotator
        {
            pitch = r.Pitch,
            yaw = r.Yaw,
            roll = r.Roll
        };
    }

    internal static Loadout ToLoadout(rlbot.flat.PlayerLoadoutT l, uint team)
    {
        Color primaryColor = l.PrimaryColorLookup switch
        {
            { } p => Color.FromArgb(p.A, p.R, p.G, p.B),
            _ => ColorSwatches.GetPrimary(l.TeamColorId, team)
        };

        Color secondaryColor = l.SecondaryColorLookup switch
        {
            { } s => Color.FromArgb(s.A, s.R, s.G, s.B),
            _ => ColorSwatches.GetSecondary(l.CustomColorId)
        };

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

        return new Loadout
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

    internal static Vector3 DesiredToVector(rlbot.flat.Vector3PartialT? partVec, Vector3 defaultVec) =>
        partVec switch
        {
            not null
                => new Vector3
                {
                    x = partVec.X?.Val ?? defaultVec.x,
                    y = partVec.Y?.Val ?? defaultVec.y,
                    z = partVec.Z?.Val ?? defaultVec.z
                },
            _ => defaultVec
        };

    internal static Rotator DesiredToRotator(rlbot.flat.RotatorPartialT? partRot, Rotator defaultRot) =>
        partRot switch
        {
            not null
                => new Rotator
                {
                    pitch = partRot.Pitch?.Val ?? defaultRot.pitch,
                    yaw = partRot.Yaw?.Val ?? defaultRot.yaw,
                    roll = partRot.Roll?.Val ?? defaultRot.roll
                },
            _ => defaultRot
        };

    internal static Physics DesiredToPhysics(rlbot.flat.DesiredPhysicsT p, Physics defaultP) =>
        new()
        {
            location = DesiredToVector(p.Location, defaultP.location),
            rotation = DesiredToRotator(p.Rotation, defaultP.rotation),
            velocity = DesiredToVector(p.Velocity, defaultP.velocity),
            angularVelocity = DesiredToVector(p.AngularVelocity, defaultP.angularVelocity),
        };
}
