using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCSTests;

[TestClass]
public class ConfigParserTest
{
    /// <summary>
    /// Fails if the action does not throw an exception which is of type T
    /// possibly wrapped in zero or more <see cref="ConfigParser.ConfigParserException"/>s.
    /// </summary>
    public static void AssertThrowsInnerException<T>(Action action)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            while (e.GetType() == typeof(ConfigParser.ConfigParserException))
            {
                if (e.InnerException == null)
                {
                    Assert.Fail();
                }
                e = e.InnerException!;
            }
            Assert.IsInstanceOfType(e, typeof(T));
            return;
        }
        Assert.Fail();
    }

    [TestMethod]
    public void EmptyVsDefaultMatchConfig()
    {
        ConfigParser parser = new();
        MatchConfigurationT defaultMC = parser.LoadMatchConfig("TestTomls/default.toml");
        MatchConfigurationT emptyMC = parser.LoadMatchConfig("TestTomls/empty.toml");

        Assert.AreEqual(emptyMC.Launcher, defaultMC.Launcher);
        Assert.AreEqual(emptyMC.LauncherArg, defaultMC.LauncherArg);
        Assert.AreEqual(emptyMC.AutoStartBots, defaultMC.AutoStartBots);
        Assert.AreEqual(emptyMC.WaitForBots, defaultMC.WaitForBots);
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
        Assert.AreEqual(emptyMutS.BoostAmount, defaultMutS.BoostAmount);
        Assert.AreEqual(emptyMutS.Rumble, defaultMutS.Rumble);
        Assert.AreEqual(emptyMutS.BoostStrength, defaultMutS.BoostStrength);
        Assert.AreEqual(emptyMutS.Gravity, defaultMutS.Gravity);
        Assert.AreEqual(emptyMutS.Demolish, defaultMutS.Demolish);
        Assert.AreEqual(emptyMutS.RespawnTime, defaultMutS.RespawnTime);
    }

    [TestMethod]
    public void EdgeCases()
    {
        ConfigParser parser = new();
        MatchConfigurationT edgeMC = parser.LoadMatchConfig("TestTomls/edge.toml");

        Assert.AreEqual(Launcher.Custom, edgeMC.Launcher);
        // Ok for parsing, but wil not go through ConfigValidator
        Assert.AreEqual("something invalid", edgeMC.LauncherArg);

        Assert.AreEqual(MatchLengthMutator.TenMinutes, edgeMC.Mutators.MatchLength);
        Assert.AreEqual(GravityMutator.Reverse, edgeMC.Mutators.Gravity);

        Assert.AreEqual("Boomer", edgeMC.PlayerConfigurations[0].Name);
        Assert.AreEqual(PlayerClass.Psyonix, edgeMC.PlayerConfigurations[0].Variety.Type);
        Assert.AreEqual(
            PsyonixSkill.Pro,
            edgeMC.PlayerConfigurations[0].Variety.AsPsyonix().BotSkill
        );
        Assert.AreEqual(null, edgeMC.PlayerConfigurations[0].Loadout);

        Assert.AreEqual("Edgy Test Bot", edgeMC.PlayerConfigurations[1].Name);
        Assert.AreEqual("", edgeMC.PlayerConfigurations[1].AgentId);
        Assert.AreEqual(PlayerClass.CustomBot, edgeMC.PlayerConfigurations[1].Variety.Type);
        Assert.AreEqual(0u, edgeMC.PlayerConfigurations[1].Team);

        Assert.AreEqual("Edgy Test Bot", edgeMC.PlayerConfigurations[2].Name);
        Assert.AreEqual(1u, edgeMC.PlayerConfigurations[2].Team);

        PlayerLoadoutT loadoutP2 = edgeMC.PlayerConfigurations[2].Loadout;
        Assert.AreEqual(69u, loadoutP2.TeamColorId);
        Assert.AreEqual(0u, loadoutP2.CustomColorId);
        Assert.AreEqual(23u, loadoutP2.CarId);
        Assert.AreEqual(6083u, loadoutP2.DecalId);
        Assert.AreEqual(1580u, loadoutP2.WheelsId);
        Assert.AreEqual(35u, loadoutP2.BoostId);
        Assert.AreEqual(0u, loadoutP2.AntennaId);
        Assert.AreEqual(0u, loadoutP2.HatId);
        Assert.AreEqual(1681u, loadoutP2.PaintFinishId);
        Assert.AreEqual(1681u, loadoutP2.CustomFinishId);
        Assert.AreEqual(5635u, loadoutP2.EngineAudioId);
        Assert.AreEqual(3220u, loadoutP2.TrailsId);
        Assert.AreEqual(4118u, loadoutP2.GoalExplosionId);
        Assert.AreEqual(12u, loadoutP2.LoadoutPaint.CarPaintId);
        Assert.AreEqual(12u, loadoutP2.LoadoutPaint.DecalPaintId);
        Assert.AreEqual(12u, loadoutP2.LoadoutPaint.WheelsPaintId);
        Assert.AreEqual(12u, loadoutP2.LoadoutPaint.BoostPaintId);
        Assert.AreEqual(0u, loadoutP2.LoadoutPaint.AntennaPaintId);
        Assert.AreEqual(0u, loadoutP2.LoadoutPaint.HatPaintId);
        Assert.AreEqual(12u, loadoutP2.LoadoutPaint.TrailsPaintId);
        Assert.AreEqual(12u, loadoutP2.LoadoutPaint.GoalExplosionPaintId);
        
        // Set to "" due to `auto_start=false`
        Assert.AreEqual("", edgeMC.PlayerConfigurations[3].RunCommand);
        Assert.AreEqual("", edgeMC.ScriptConfigurations[0].RunCommand);
    }

    [TestMethod]
    public void EmptyVsDefaultBotAndScriptToml()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/empty_agents.toml");

        PlayerConfigurationT player = mc.PlayerConfigurations[0];
        Assert.AreEqual("", player.Name);
        Assert.AreEqual("", player.AgentId);
        Assert.AreEqual(0u, player.Team);
        Assert.AreEqual(PlayerClass.CustomBot, player.Variety.Type);
        Assert.AreEqual(Path.GetFullPath("TestTomls"), player.RootDir);
        Assert.AreEqual("", player.RunCommand);
        Assert.AreEqual(null, player.Loadout);
        Assert.AreEqual(false, player.Hivemind);

        ScriptConfigurationT script = mc.ScriptConfigurations[0];
        Assert.AreEqual("", script.Name);
        Assert.AreEqual("", script.AgentId);
        Assert.AreEqual(Path.GetFullPath("TestTomls"), script.RootDir);
        Assert.AreEqual("", script.RunCommand);
    }

    [TestMethod]
    public void Overrides()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/overrides.toml");

        PlayerConfigurationT player = mc.PlayerConfigurations[0];
        Assert.AreEqual("New Bot Name", player.Name);
        Assert.AreEqual(null, player.Loadout);

        ScriptConfigurationT script = mc.ScriptConfigurations[0];
        Assert.AreEqual("Normal Test Script", script.Name); // Not overriden
    }

    [TestMethod]
    public void ConfigNotFound()
    {
        ConfigParser parser = new ConfigParser();
        AssertThrowsInnerException<ArgumentNullException>(() => parser.LoadMatchConfig(null!));

        AssertThrowsInnerException<FileNotFoundException>(
            () => parser.LoadMatchConfig("TestTomls/non-existent.toml")
        );

        // Match toml exists, but refers to bot that does not exist
        Assert.IsTrue(Path.Exists("TestTomls/non-existent_bot.toml"));
        AssertThrowsInnerException<FileNotFoundException>(
            () => parser.LoadMatchConfig("TestTomls/non-existent_bot.toml")
        );

        // Match toml exists, but refers to script that does not exist
        Assert.IsTrue(Path.Exists("TestTomls/non-existent_script.toml"));
        AssertThrowsInnerException<FileNotFoundException>(
            () => parser.LoadMatchConfig("TestTomls/non-existent_script.toml")
        );
    }

    [TestMethod]
    public void InvalidTomlConfig()
    {
        ConfigParser parser = new();
        AssertThrowsInnerException<Tomlyn.TomlException>(
            () => parser.LoadMatchConfig("TestTomls/not_a_toml.json")
        );
    }

    [TestMethod]
    public void BadValues()
    {
        ConfigParser parser = new();
        AssertThrowsInnerException<InvalidCastException>(
            () => parser.LoadMatchConfig("TestTomls/bad_boolean.toml")
        );
        AssertThrowsInnerException<InvalidCastException>(
            () => parser.LoadMatchConfig("TestTomls/bad_enum.toml")
        );
        AssertThrowsInnerException<InvalidCastException>(
            () => parser.LoadMatchConfig("TestTomls/bad_team.toml")
        );
    }
}
