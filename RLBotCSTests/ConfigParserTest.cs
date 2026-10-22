using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RLBot.Flat;
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
            Assert.IsInstanceOfType<T>(e);
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
        Assert.AreEqual(emptyMC.AutoStartAgents, defaultMC.AutoStartAgents);
        Assert.AreEqual(emptyMC.WaitForAgents, defaultMC.WaitForAgents);
        Assert.AreEqual(emptyMC.GameMapUpk, defaultMC.GameMapUpk);
        Assert.HasCount(emptyMC.PlayerConfigurations.Count, defaultMC.PlayerConfigurations);
        Assert.HasCount(emptyMC.ScriptConfigurations.Count, defaultMC.ScriptConfigurations);
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

        Assert.AreEqual(DebugRendering.AlwaysOff, edgeMC.EnableRendering);
        Assert.AreEqual(Launcher.Custom, edgeMC.Launcher);
        // Ok for parsing, but wil not go through ConfigValidator
        Assert.AreEqual("something invalid", edgeMC.LauncherArg);

        Assert.AreEqual(MatchLengthMutator.TenMinutes, edgeMC.Mutators.MatchLength);
        Assert.AreEqual(GravityMutator.Reverse, edgeMC.Mutators.Gravity);

        Assert.AreEqual(PlayerClass.PsyonixBot, edgeMC.PlayerConfigurations[0].Variety.Type);
        Assert.AreEqual("Boomer", edgeMC.PlayerConfigurations[0].Variety.AsPsyonixBot().Name);
        Assert.AreEqual(
            PsyonixSkill.Pro,
            edgeMC.PlayerConfigurations[0].Variety.AsPsyonixBot().BotSkill
        );
        Assert.IsNull(edgeMC.PlayerConfigurations[0].Variety.AsPsyonixBot().Loadout);

        Assert.AreEqual(PlayerClass.CustomBot, edgeMC.PlayerConfigurations[1].Variety.Type);
        Assert.AreEqual(
            "Edgy Test Bot",
            edgeMC.PlayerConfigurations[1].Variety.AsCustomBot().Name
        );
        Assert.AreEqual("", edgeMC.PlayerConfigurations[1].Variety.AsCustomBot().AgentId);
        Assert.AreEqual(0u, edgeMC.PlayerConfigurations[1].Team);

        Assert.AreEqual(
            "Edgy Test Bot",
            edgeMC.PlayerConfigurations[2].Variety.AsCustomBot().Name
        );
        Assert.AreEqual(1u, edgeMC.PlayerConfigurations[2].Team);

        PlayerLoadoutT loadoutP2 = edgeMC
            .PlayerConfigurations[2]
            .Variety.AsCustomBot()
            .Loadout;
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
        Assert.AreEqual("", edgeMC.PlayerConfigurations[3].Variety.AsCustomBot().RunCommand);
        Assert.AreEqual("", edgeMC.ScriptConfigurations[0].RunCommand);
    }

    [TestMethod]
    public void EmptyVsDefaultBotAndScriptToml()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/empty_agents.toml");

        PlayerConfigurationT player = mc.PlayerConfigurations[0];
        Assert.AreEqual(PlayerClass.CustomBot, player.Variety.Type);
        Assert.AreEqual("", player.Variety.AsCustomBot().Name);
        Assert.AreEqual("", player.Variety.AsCustomBot().AgentId);
        Assert.AreEqual(0u, player.Team);
        Assert.AreEqual(Path.GetFullPath("TestTomls"), player.Variety.AsCustomBot().RootDir);
        Assert.AreEqual("", player.Variety.AsCustomBot().RunCommand);
        Assert.IsNull(player.Variety.AsCustomBot().Loadout);
        Assert.IsFalse(player.Variety.AsCustomBot().Hivemind);

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
        Assert.AreEqual("New Bot Name", player.Variety.AsCustomBot().Name);
        CustomBotT bot = player.Variety.AsCustomBot();
        Assert.IsNull(bot.Loadout);
        Assert.AreEqual("bot-value", bot.Environment.Single(e => e.Name == "BOT_ENV").Value);
        Assert.AreEqual(
            "bot-shared",
            bot.Environment.Single(e => e.Name == "SHARED_ENV").Value
        );

        ScriptConfigurationT script = mc.ScriptConfigurations[0];
        Assert.AreEqual("Normal Test Script", script.Name); // Not overriden
        Assert.AreEqual(
            "script-value",
            script.Environment.Single(e => e.Name == "SCRIPT_ENV").Value
        );
        Assert.AreEqual(
            "script-shared",
            script.Environment.Single(e => e.Name == "SHARED_ENV").Value
        );
    }

    [TestMethod]
    public void PlatformEnvironmentOverrides()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/platform.toml");

        string expectedShared =
            OperatingSystem.IsWindows() ? "windows-bot-value"
            : OperatingSystem.IsLinux() ? "linux-bot-value"
            : "common-bot-value";
        string expectedScriptShared =
            OperatingSystem.IsWindows() ? "windows-script-value"
            : OperatingSystem.IsLinux() ? "linux-script-value"
            : "common-script-value";

        CustomBotT bot = mc.PlayerConfigurations[0].Variety.AsCustomBot();
        Assert.AreEqual(
            "$HOME/common-value",
            bot.Environment.Single(e => e.Name == "COMMON_ENV").Value
        );
        Assert.AreEqual(
            expectedShared,
            bot.Environment.Single(e => e.Name == "SHARED_ENV").Value
        );

        ScriptConfigurationT script = mc.ScriptConfigurations[0];
        Assert.AreEqual(
            "$HOME/common-value",
            script.Environment.Single(e => e.Name == "COMMON_ENV").Value
        );
        Assert.AreEqual(
            expectedScriptShared,
            script.Environment.Single(e => e.Name == "SHARED_ENV").Value
        );

        if (OperatingSystem.IsWindows())
        {
            Assert.AreEqual(
                Environment.ExpandEnvironmentVariables(
                    "%LOCALAPPDATA%\\RLBot5\\bots\\torch-archive"
                ),
                bot.Environment.Single(e => e.Name == "PLATFORM_ENV").Value
            );
            Assert.AreEqual(
                Environment.ExpandEnvironmentVariables(
                    "%LOCALAPPDATA%\\RLBot5\\bots\\torch-archive"
                ),
                script.Environment.Single(e => e.Name == "PLATFORM_ENV").Value
            );
        }
        else if (OperatingSystem.IsLinux())
        {
            string expectedPlatform =
                $"{Environment.GetEnvironmentVariable("HOME")}/.local/share/RLBot5/bots/torch-archive";
            Assert.AreEqual(
                expectedPlatform,
                bot.Environment.Single(e => e.Name == "PLATFORM_ENV").Value
            );
            Assert.AreEqual(
                expectedPlatform,
                script.Environment.Single(e => e.Name == "PLATFORM_ENV").Value
            );
        }
        else
        {
            Assert.IsFalse(bot.Environment.Any(e => e.Name == "PLATFORM_ENV"));
            Assert.IsFalse(script.Environment.Any(e => e.Name == "PLATFORM_ENV"));
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void ExpandsSelectedPlatformEnvironmentValuesOnce()
    {
        string[] names =
        [
            "RLBCPERCENT_TEST",
            "RLBCEMPTY_TEST",
            "RLBCDOLLAR_TEST",
            "RLBCPERCENT_REPLACEMENT_TEST",
            "RLBCPAREN_TEST(x86)",
            "RLBCINNER_TEST",
            "RLBCMISSING_TEST",
        ];
        string?[] previousValues = names.Select(Environment.GetEnvironmentVariable).ToArray();
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            Environment.SetEnvironmentVariable("RLBCPERCENT_TEST", "percent-value");
            Environment.SetEnvironmentVariable("RLBCEMPTY_TEST", "");
            Assert.AreEqual("", Environment.GetEnvironmentVariable("RLBCEMPTY_TEST"));
            Environment.SetEnvironmentVariable("RLBCDOLLAR_TEST", "$RLBCINNER_TEST");
            Environment.SetEnvironmentVariable(
                "RLBCPERCENT_REPLACEMENT_TEST",
                "%RLBCINNER_TEST%"
            );
            Environment.SetEnvironmentVariable("RLBCPAREN_TEST(x86)", "paren-value");
            Environment.SetEnvironmentVariable("RLBCINNER_TEST", "expanded");
            Environment.SetEnvironmentVariable("RLBCMISSING_TEST", null);
            Assert.IsNull(Environment.GetEnvironmentVariable("RLBCMISSING_TEST"));

            string platform = OperatingSystem.IsWindows() ? "windows" : "linux";
            string matchPath = Path.Combine(directory, "match.toml");
            File.WriteAllText(
                matchPath,
                """
                [match]
                [[cars]]
                config_file = "bot.toml"
                [[scripts]]
                config_file = "script.toml"
                """
            );
            string agentToml = $$"""
                [settings]
                agent_id = "test"
                name = "test"
                run_command = ""
                run_command_linux = ""

                [settings.environment]
                COMMON = "$HOME and %RLBCPERCENT_TEST%"

                [settings.environment.{{platform}}]
                VALUE = "%RLBCPERCENT_TEST%$RLBCEMPTY_TEST${RLBCPERCENT_TEST}%RLBCPAREN_TEST(x86)%%RLBCMISSING_TEST%$RLBCMISSING_TEST${RLBCMISSING_TEST}"
                DOLLAR_REPLACEMENT = "%RLBCDOLLAR_TEST%"
                PERCENT_REPLACEMENT = "$RLBCPERCENT_REPLACEMENT_TEST"
                """;
            File.WriteAllText(Path.Combine(directory, "bot.toml"), agentToml);
            File.WriteAllText(Path.Combine(directory, "script.toml"), agentToml);

            MatchConfigurationT config = new ConfigParser().LoadMatchConfig(matchPath);
            CustomBotT bot = config.PlayerConfigurations[0].Variety.AsCustomBot();
            ScriptConfigurationT script = config.ScriptConfigurations[0];

            AssertEnvironmentValue(
                bot.Environment,
                "VALUE",
                "percent-valuepercent-valueparen-value%RLBCMISSING_TEST%$RLBCMISSING_TEST${RLBCMISSING_TEST}"
            );
            AssertEnvironmentValue(
                script.Environment,
                "VALUE",
                "percent-valuepercent-valueparen-value%RLBCMISSING_TEST%$RLBCMISSING_TEST${RLBCMISSING_TEST}"
            );
            AssertEnvironmentValue(bot.Environment, "DOLLAR_REPLACEMENT", "$RLBCINNER_TEST");
            AssertEnvironmentValue(
                script.Environment,
                "DOLLAR_REPLACEMENT",
                "$RLBCINNER_TEST"
            );
            AssertEnvironmentValue(bot.Environment, "PERCENT_REPLACEMENT", "%RLBCINNER_TEST%");
            AssertEnvironmentValue(
                script.Environment,
                "PERCENT_REPLACEMENT",
                "%RLBCINNER_TEST%"
            );
            AssertEnvironmentValue(bot.Environment, "COMMON", "$HOME and %RLBCPERCENT_TEST%");
            AssertEnvironmentValue(
                script.Environment,
                "COMMON",
                "$HOME and %RLBCPERCENT_TEST%"
            );
        }
        finally
        {
            for (int i = 0; i < names.Length; i++)
                Environment.SetEnvironmentVariable(names[i], previousValues[i]);
            Directory.Delete(directory, true);
        }
    }

    private static void AssertEnvironmentValue(
        System.Collections.Generic.IEnumerable<EnvironmentVariableT> environment,
        string name,
        string expected
    )
    {
        Assert.AreEqual(expected, environment.Single(value => value.Name == name).Value);
    }

    [TestMethod]
    public void ConfigNotFound()
    {
        ConfigParser parser = new();
        AssertThrowsInnerException<ArgumentNullException>(() => parser.LoadMatchConfig(null!));

        AssertThrowsInnerException<FileNotFoundException>(() =>
            parser.LoadMatchConfig("TestTomls/non-existent.toml")
        );

        // Match toml exists, but refers to bot that does not exist
        Assert.IsTrue(Path.Exists("TestTomls/non-existent_bot.toml"));
        AssertThrowsInnerException<FileNotFoundException>(() =>
            parser.LoadMatchConfig("TestTomls/non-existent_bot.toml")
        );

        // Match toml exists, but refers to script that does not exist
        Assert.IsTrue(Path.Exists("TestTomls/non-existent_script.toml"));
        AssertThrowsInnerException<FileNotFoundException>(() =>
            parser.LoadMatchConfig("TestTomls/non-existent_script.toml")
        );
    }

    [TestMethod]
    public void InvalidTomlConfig()
    {
        ConfigParser parser = new();
        AssertThrowsInnerException<Tomlyn.TomlException>(() =>
            parser.LoadMatchConfig("TestTomls/not_a_toml.json")
        );
    }

    [TestMethod]
    public void BadValues()
    {
        ConfigParser parser = new();
        AssertThrowsInnerException<InvalidCastException>(() =>
            parser.LoadMatchConfig("TestTomls/bad_boolean.toml")
        );
        AssertThrowsInnerException<InvalidCastException>(() =>
            parser.LoadMatchConfig("TestTomls/bad_enum.toml")
        );
        AssertThrowsInnerException<InvalidCastException>(() =>
            parser.LoadMatchConfig("TestTomls/bad_team.toml")
        );
    }
}
