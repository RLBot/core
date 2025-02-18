using Microsoft.VisualStudio.TestTools.UnitTesting;
using rlbot.flat;
using RLBotCS.ManagerTools;

namespace RLBotCSTests;

[TestClass]
public class ConfigValidatorTest
{
    [TestMethod]
    public void DefaultIsValid()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/default.toml");
        Assert.IsTrue(ConfigValidator.Validate(mc));
    }

    [TestMethod]
    public void OverridesAreValid()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/overrides.toml");
        Assert.IsTrue(ConfigValidator.Validate(mc));
    }

    [TestMethod]
    public void UnknownLauncherArg()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/edge.toml");
        foreach (var player in mc.PlayerConfigurations)
        {
            player.AgentId = "test/" + player.Name;
        }
        Assert.IsFalse(ConfigValidator.Validate(mc));
        mc.LauncherArg = "legendary";
        Assert.IsTrue(ConfigValidator.Validate(mc));
    }

    [TestMethod]
    public void EmptyAgentIds()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/empty_agents.toml");
        Assert.IsFalse(ConfigValidator.Validate(mc));
    }

    [TestMethod]
    public void MultipleHumans()
    {
        ConfigParser parser = new();
        MatchConfigurationT mc = parser.LoadMatchConfig("TestTomls/multi_human.toml");
        Assert.IsFalse(ConfigValidator.Validate(mc));

        // Otherwise ok
        MatchConfigurationT mc2 = parser.LoadMatchConfig("TestTomls/multi_human.toml");
        mc2.PlayerConfigurations[0].Variety = PlayerClassUnion.FromPsyonix(new PsyonixT());
        Assert.IsTrue(ConfigValidator.Validate(mc2));
    }
}
