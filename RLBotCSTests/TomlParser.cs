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
    }
}
