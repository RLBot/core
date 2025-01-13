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
        MatchConfigurationT defaultMC = ConfigParser.GetMatchConfig("TomlTest/default.toml");
        MatchConfigurationT emptyMC = ConfigParser.GetMatchConfig("TomlTest/empty.toml");
        MatchConfigurationT edgeMC = ConfigParser.GetMatchConfig("TomlTest/edge.toml");

        Assert.AreEqual(emptyMC.Launcher, defaultMC.Launcher);
        Assert.AreEqual(emptyMC.LauncherArg, defaultMC.LauncherArg);
        Assert.AreEqual(emptyMC.AutoStartBots, defaultMC.AutoStartBots);
        Assert.AreEqual(emptyMC.GameMapUpk, defaultMC.GameMapUpk);
        Assert.AreEqual(
            emptyMC.PlayerConfigurations.Count,
            defaultMC.PlayerConfigurations.Count
        );
        Assert.AreEqual(
            emptyMC.ScriptConfigurations.Count,
            defaultMC.ScriptConfigurations.Count
        );
        Assert.AreEqual(emptyMC.GameMode, defaultMC.GameMode);
        Assert.AreEqual(emptyMC.SkipReplays, defaultMC.SkipReplays);
        Assert.AreEqual(emptyMC.InstantStart, defaultMC.InstantStart);
        Assert.AreEqual(emptyMC.ExistingMatchBehavior, defaultMC.ExistingMatchBehavior);
        Assert.AreEqual(emptyMC.EnableRendering, defaultMC.EnableRendering);
        Assert.AreEqual(emptyMC.EnableStateSetting, defaultMC.EnableStateSetting);
        Assert.AreEqual(emptyMC.AutoSaveReplay, defaultMC.AutoSaveReplay);
        Assert.AreEqual(emptyMC.Freeplay, defaultMC.Freeplay);

        MutatorSettingsT defaultMutS = defaultMC.Mutators;
        MutatorSettingsT emptyMutS = emptyMC.Mutators;

        Assert.AreEqual(emptyMutS.MatchLength, defaultMutS.MatchLength);
        Assert.AreEqual(emptyMutS.MaxScore, defaultMutS.MaxScore);
        Assert.AreEqual(emptyMutS.MultiBall, defaultMutS.MultiBall);
        Assert.AreEqual(emptyMutS.Overtime, defaultMutS.Overtime);
        Assert.AreEqual(emptyMutS.SeriesLength, defaultMutS.SeriesLength);
        Assert.AreEqual(emptyMutS.GameSpeed, defaultMutS.GameSpeed);
        Assert.AreEqual(emptyMutS.BallMaxSpeed, defaultMutS.BallMaxSpeed);
        Assert.AreEqual(emptyMutS.BallType, defaultMutS.BallType);
        Assert.AreEqual(emptyMutS.BallWeight, defaultMutS.BallWeight);
        Assert.AreEqual(emptyMutS.BallSize, defaultMutS.BallSize);
        Assert.AreEqual(emptyMutS.BallBounciness, defaultMutS.BallBounciness);
        Assert.AreEqual(emptyMutS.Boost, defaultMutS.Boost);
        Assert.AreEqual(emptyMutS.Rumble, defaultMutS.Rumble);
        Assert.AreEqual(emptyMutS.BoostStrength, defaultMutS.BoostStrength);
        Assert.AreEqual(emptyMutS.Gravity, defaultMutS.Gravity);
        Assert.AreEqual(emptyMutS.Demolish, defaultMutS.Demolish);
        Assert.AreEqual(emptyMutS.RespawnTime, defaultMutS.RespawnTime);

        Assert.AreEqual(Launcher.Custom, edgeMC.Launcher);
        Assert.AreEqual("legendary", edgeMC.LauncherArg);
        Assert.AreEqual(MatchLengthMutator.FiveMinutes, edgeMC.Mutators.MatchLength);

        Assert.AreEqual("Boomer", edgeMC.PlayerConfigurations[0].Name);
        Assert.AreEqual(PlayerClass.Psyonix, edgeMC.PlayerConfigurations[0].Variety.Type);
        Assert.AreEqual(292u, edgeMC.PlayerConfigurations[0].Loadout.DecalId);

        Assert.AreEqual("Test Bot", edgeMC.PlayerConfigurations[1].Name);
        Assert.AreEqual(PlayerClass.CustomBot, edgeMC.PlayerConfigurations[1].Variety.Type);
        Assert.AreEqual(69u, edgeMC.PlayerConfigurations[1].Loadout.TeamColorId);
        Assert.AreEqual(0u, edgeMC.PlayerConfigurations[1].Loadout.CustomColorId);
        Assert.AreEqual(23u, edgeMC.PlayerConfigurations[1].Loadout.CarId);
        Assert.AreEqual(6083u, edgeMC.PlayerConfigurations[1].Loadout.DecalId);
        Assert.AreEqual(1580u, edgeMC.PlayerConfigurations[1].Loadout.WheelsId);
        Assert.AreEqual(35u, edgeMC.PlayerConfigurations[1].Loadout.BoostId);
        Assert.AreEqual(0u, edgeMC.PlayerConfigurations[1].Loadout.AntennaId);
        Assert.AreEqual(0u, edgeMC.PlayerConfigurations[1].Loadout.HatId);
        Assert.AreEqual(1681u, edgeMC.PlayerConfigurations[1].Loadout.PaintFinishId);
        Assert.AreEqual(1681u, edgeMC.PlayerConfigurations[1].Loadout.CustomFinishId);
        Assert.AreEqual(5635u, edgeMC.PlayerConfigurations[1].Loadout.EngineAudioId);
        Assert.AreEqual(3220u, edgeMC.PlayerConfigurations[1].Loadout.TrailsId);
        Assert.AreEqual(4118u, edgeMC.PlayerConfigurations[1].Loadout.GoalExplosionId);
        Assert.AreEqual(12u, edgeMC.PlayerConfigurations[1].Loadout.LoadoutPaint.CarPaintId);
        Assert.AreEqual(12u, edgeMC.PlayerConfigurations[1].Loadout.LoadoutPaint.DecalPaintId);
        Assert.AreEqual(
            12u,
            edgeMC.PlayerConfigurations[1].Loadout.LoadoutPaint.WheelsPaintId
        );
        Assert.AreEqual(12u, edgeMC.PlayerConfigurations[1].Loadout.LoadoutPaint.BoostPaintId);
        Assert.AreEqual(
            0u,
            edgeMC.PlayerConfigurations[1].Loadout.LoadoutPaint.AntennaPaintId
        );
        Assert.AreEqual(0u, edgeMC.PlayerConfigurations[1].Loadout.LoadoutPaint.HatPaintId);
        Assert.AreEqual(
            12u,
            edgeMC.PlayerConfigurations[1].Loadout.LoadoutPaint.TrailsPaintId
        );
        Assert.AreEqual(
            12u,
            edgeMC.PlayerConfigurations[1].Loadout.LoadoutPaint.GoalExplosionPaintId
        );
    }
}
