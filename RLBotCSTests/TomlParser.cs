using Microsoft.VisualStudio.TestTools.UnitTesting;
using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCSTests;

[TestClass]
public class TomlParser
{
    [TestMethod]
    public void TestParse()
    {
        MatchSettingsT defaultMS = ConfigParser.GetMatchSettings("TomlTest/default.toml");
        MatchSettingsT emptyMS = ConfigParser.GetMatchSettings("TomlTest/empty.toml");
        MatchSettingsT edgeMS = ConfigParser.GetMatchSettings("TomlTest/edge.toml");

        Assert.AreEqual(emptyMS.Launcher, defaultMS.Launcher);
        Assert.AreEqual(emptyMS.GamePath, defaultMS.GamePath);
        Assert.AreEqual(emptyMS.AutoStartBots, defaultMS.AutoStartBots);
        Assert.AreEqual(emptyMS.GameMapUpk, defaultMS.GameMapUpk);
        Assert.AreEqual(
            emptyMS.PlayerConfigurations.Count,
            defaultMS.PlayerConfigurations.Count
        );
        Assert.AreEqual(
            emptyMS.ScriptConfigurations.Count,
            defaultMS.ScriptConfigurations.Count
        );
        Assert.AreEqual(emptyMS.GameMode, defaultMS.GameMode);
        Assert.AreEqual(emptyMS.SkipReplays, defaultMS.SkipReplays);
        Assert.AreEqual(emptyMS.InstantStart, defaultMS.InstantStart);
        Assert.AreEqual(emptyMS.ExistingMatchBehavior, defaultMS.ExistingMatchBehavior);
        Assert.AreEqual(emptyMS.EnableRendering, defaultMS.EnableRendering);
        Assert.AreEqual(emptyMS.EnableStateSetting, defaultMS.EnableStateSetting);
        Assert.AreEqual(emptyMS.AutoSaveReplay, defaultMS.AutoSaveReplay);
        Assert.AreEqual(emptyMS.Freeplay, defaultMS.Freeplay);

        MutatorSettingsT defaultMutS = defaultMS.MutatorSettings;
        MutatorSettingsT emptyMutS = emptyMS.MutatorSettings;

        Assert.AreEqual(emptyMutS.MatchLength, defaultMutS.MatchLength);
        Assert.AreEqual(emptyMutS.MaxScore, defaultMutS.MaxScore);
        Assert.AreEqual(emptyMutS.MultiBall, defaultMutS.MultiBall);
        Assert.AreEqual(emptyMutS.OvertimeOption, defaultMutS.OvertimeOption);
        Assert.AreEqual(emptyMutS.SeriesLengthOption, defaultMutS.SeriesLengthOption);
        Assert.AreEqual(emptyMutS.GameSpeedOption, defaultMutS.GameSpeedOption);
        Assert.AreEqual(emptyMutS.BallMaxSpeedOption, defaultMutS.BallMaxSpeedOption);
        Assert.AreEqual(emptyMutS.BallTypeOption, defaultMutS.BallTypeOption);
        Assert.AreEqual(emptyMutS.BallWeightOption, defaultMutS.BallWeightOption);
        Assert.AreEqual(emptyMutS.BallSizeOption, defaultMutS.BallSizeOption);
        Assert.AreEqual(emptyMutS.BallBouncinessOption, defaultMutS.BallBouncinessOption);
        Assert.AreEqual(emptyMutS.BoostOption, defaultMutS.BoostOption);
        Assert.AreEqual(emptyMutS.RumbleOption, defaultMutS.RumbleOption);
        Assert.AreEqual(emptyMutS.BoostStrengthOption, defaultMutS.BoostStrengthOption);
        Assert.AreEqual(emptyMutS.GravityOption, defaultMutS.GravityOption);
        Assert.AreEqual(emptyMutS.DemolishOption, defaultMutS.DemolishOption);
        Assert.AreEqual(emptyMutS.RespawnTimeOption, defaultMutS.RespawnTimeOption);

        Assert.AreEqual(Launcher.Custom, edgeMS.Launcher);
        Assert.AreEqual("legendary", edgeMS.GamePath);
        Assert.AreEqual(MatchLength.Five_Minutes, edgeMS.MutatorSettings.MatchLength);

        Assert.AreEqual("Boomer", edgeMS.PlayerConfigurations[0].Name);
        Assert.AreEqual(PlayerClass.Psyonix, edgeMS.PlayerConfigurations[0].Variety.Type);
        Assert.AreEqual(292u, edgeMS.PlayerConfigurations[0].Loadout.DecalId);

        Assert.AreEqual("Test Bot", edgeMS.PlayerConfigurations[1].Name);
        Assert.AreEqual(PlayerClass.CustomBot, edgeMS.PlayerConfigurations[1].Variety.Type);
        Assert.AreEqual(69u, edgeMS.PlayerConfigurations[1].Loadout.TeamColorId);
        Assert.AreEqual(0u, edgeMS.PlayerConfigurations[1].Loadout.CustomColorId);
        Assert.AreEqual(23u, edgeMS.PlayerConfigurations[1].Loadout.CarId);
        Assert.AreEqual(6083u, edgeMS.PlayerConfigurations[1].Loadout.DecalId);
        Assert.AreEqual(1580u, edgeMS.PlayerConfigurations[1].Loadout.WheelsId);
        Assert.AreEqual(35u, edgeMS.PlayerConfigurations[1].Loadout.BoostId);
        Assert.AreEqual(0u, edgeMS.PlayerConfigurations[1].Loadout.AntennaId);
        Assert.AreEqual(0u, edgeMS.PlayerConfigurations[1].Loadout.HatId);
        Assert.AreEqual(1681u, edgeMS.PlayerConfigurations[1].Loadout.PaintFinishId);
        Assert.AreEqual(1681u, edgeMS.PlayerConfigurations[1].Loadout.CustomFinishId);
        Assert.AreEqual(5635u, edgeMS.PlayerConfigurations[1].Loadout.EngineAudioId);
        Assert.AreEqual(3220u, edgeMS.PlayerConfigurations[1].Loadout.TrailsId);
        Assert.AreEqual(4118u, edgeMS.PlayerConfigurations[1].Loadout.GoalExplosionId);
        Assert.AreEqual(12u, edgeMS.PlayerConfigurations[1].Loadout.LoadoutPaint.CarPaintId);
        Assert.AreEqual(12u, edgeMS.PlayerConfigurations[1].Loadout.LoadoutPaint.DecalPaintId);
        Assert.AreEqual(
            12u,
            edgeMS.PlayerConfigurations[1].Loadout.LoadoutPaint.WheelsPaintId
        );
        Assert.AreEqual(12u, edgeMS.PlayerConfigurations[1].Loadout.LoadoutPaint.BoostPaintId);
        Assert.AreEqual(
            0u,
            edgeMS.PlayerConfigurations[1].Loadout.LoadoutPaint.AntennaPaintId
        );
        Assert.AreEqual(0u, edgeMS.PlayerConfigurations[1].Loadout.LoadoutPaint.HatPaintId);
        Assert.AreEqual(
            12u,
            edgeMS.PlayerConfigurations[1].Loadout.LoadoutPaint.TrailsPaintId
        );
        Assert.AreEqual(
            12u,
            edgeMS.PlayerConfigurations[1].Loadout.LoadoutPaint.GoalExplosionPaintId
        );
    }
}
