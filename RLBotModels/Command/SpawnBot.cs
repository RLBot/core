using System.Drawing;
using RLBotModels.Message;

namespace RLBotModels.Command
{
    public struct SpawnBot
    {
        public string name;
        byte team;
        BotSkill botType;
    }

    public struct Loadout
    {
        public ushort carId;
        public ushort decalId;
        public ushort wheelsId;
        public ushort boostId;
        public ushort antennaId;
        public ushort hatId;
        public ushort paintFinishId;
        public ushort customFinishId;
        public ushort engineAudioId;
        public ushort trailsId;
        public ushort goalExplosionId;

        public LoadoutPaint loadoutPaint;

        /// Sets the primary color of the car to the swatch that most closely matches the provided
        /// RGB color value. If set, this overrides teamColorId.
        public Color primaryColorLookup;

        /// Sets the secondary color of the car to the swatch that most closely matches the provided
        /// RGB color value. If set, this overrides customColorId.
        public Color secondaryColorLookup;
    }

    public struct LoadoutPaint
    {
        public byte carPaintId;
        public byte decalPaintId;
        public byte wheelsPaintId;
        public byte boostPaintId;
        public byte antennaPaintId;
        public byte hatPaintId;
        public byte trailsPaintId;
        public byte goalExplosionPaintId;
    }
}
