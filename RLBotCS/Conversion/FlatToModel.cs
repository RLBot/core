using System.Drawing;
using Bridge.Models.Command;
using Bridge.Models.Control;
using Bridge.Models.Phys;
using Bridge.Models.Render;
using Bridge.State;

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

    internal static Vector3 ToVectorFromT(rlbot.flat.Vector3T vec) => new(vec.X, vec.Y, vec.Z);

    internal static RelativeAnchor ExtractRelativeAnchor(
        rlbot.flat.RelativeAnchorUnion? relative,
        GameState gameState
    ) =>
        relative?.Value switch
        {
            rlbot.flat.CarAnchorT { Index: var index, Local: var local }
                => (
                    new RelativeAnchor()
                    {
                        ActorId = gameState.PlayerMapping.ActorIdFromPlayerIndex(index) ?? 0,
                        Local = ToVectorFromT(local)
                    }
                ),
            rlbot.flat.BallAnchorT { Index: var index, Local: var local }
                => new RelativeAnchor()
                {
                    ActorId = gameState.GetBallActorIdFromIndex(index) ?? 0,
                    Local = ToVectorFromT(local)
                },
            _ => new RelativeAnchor(),
        };

    internal static RenderAnchor ToRenderAnchor(
        rlbot.flat.RenderAnchorT offset,
        GameState gameState
    ) =>
        new RenderAnchor
        {
            World = ToVectorFromT(offset.World),
            Relative = ExtractRelativeAnchor(offset.Relative, gameState)
        };

    internal static Color ToColor(rlbot.flat.ColorT c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    internal static Rotator ToRotator(rlbot.flat.Rotator r) =>
        new Rotator(r.Pitch, r.Yaw, r.Roll);

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

    internal static Vector3 DesiredToVector(
        rlbot.flat.Vector3PartialT? partVec,
        Vector3 defaultVec
    ) =>
        partVec switch
        {
            not null
                => new Vector3(
                    partVec.X?.Val ?? defaultVec.X,
                    partVec.Y?.Val ?? defaultVec.Y,
                    partVec.Z?.Val ?? defaultVec.Z
                ),
            _ => defaultVec
        };

    internal static Rotator DesiredToRotator(
        rlbot.flat.RotatorPartialT? partRot,
        Rotator defaultRot
    ) =>
        partRot switch
        {
            not null
                => new Rotator(
                    partRot.Pitch?.Val ?? defaultRot.Pitch,
                    partRot.Yaw?.Val ?? defaultRot.Yaw,
                    partRot.Roll?.Val ?? defaultRot.Roll
                ),
            _ => defaultRot
        };

    internal static Physics DesiredToPhysics(rlbot.flat.DesiredPhysicsT p, Physics defaultP) =>
        new(
            DesiredToVector(p.Location, defaultP.Location),
            DesiredToVector(p.Velocity, defaultP.Velocity),
            DesiredToVector(p.AngularVelocity, defaultP.AngularVelocity),
            DesiredToRotator(p.Rotation, defaultP.Rotation)
        );
}
