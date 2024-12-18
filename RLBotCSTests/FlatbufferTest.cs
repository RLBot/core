using System;
using System.Collections.Generic;
using System.Linq;
using Google.FlatBuffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using rlbot.flat;

namespace RLBotCSTests;

[TestClass]
public class FlatbufferTest
{
    public static string RandomString(int length)
    {
        Random random = new();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray()
        );
    }

    public static List<PlayerConfigurationT> RandomPlayerConfigurations()
    {
        List<PlayerConfigurationT> playerConfigurations = new();
        for (int i = 0; i < 128; i++)
        {
            playerConfigurations.Add(
                new PlayerConfigurationT()
                {
                    Variety = PlayerClassUnion.FromPsyonix(new PsyonixT()),
                    Name = RandomString(64),
                    AgentId = RandomString(64),
                    RootDir = RandomString(64),
                    RunCommand = RandomString(64),
                    Loadout = new PlayerLoadoutT() { LoadoutPaint = new LoadoutPaintT() },
                }
            );
        }

        return playerConfigurations;
    }

    public static List<ScriptConfigurationT> RandomScriptConfigurations()
    {
        List<ScriptConfigurationT> scriptConfigurations = new();
        for (int i = 0; i < 128; i++)
        {
            scriptConfigurations.Add(
                new ScriptConfigurationT()
                {
                    Name = RandomString(64),
                    AgentId = RandomString(64),
                    Location = RandomString(64),
                    RunCommand = RandomString(64),
                }
            );
        }

        return scriptConfigurations;
    }

    [TestMethod]
    public void TestBufferGrow()
    {
        MatchSettingsT matchSettings = new MatchSettingsT()
        {
            GamePath = RandomString(64),
            GameMapUpk = RandomString(64),
            MutatorSettings = new MutatorSettingsT(),
            PlayerConfigurations = RandomPlayerConfigurations(),
            ScriptConfigurations = RandomScriptConfigurations(),
        };

        // only allocate 1 byte
        FlatBufferBuilder builderSerialize = new(1);
        var offset = MatchSettings.Pack(builderSerialize, matchSettings);
        builderSerialize.Finish(offset.Value);
        // if we got here, the buffer grew successfully
    }
}
