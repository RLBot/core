using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
    public static void LaunchBots(
        List<RLBot.Flat.PlayerConfigurationT> bots,
        int rlbotSocketsPort
    )
    {
        foreach (var bot in bots)
        {
            var details = bot.Variety.AsCustomBot();

            if (details.RunCommand == "")
            {
                Logger.LogWarning("Bot {} must be started manually.", details.Name);
                continue;
            }

            Process botProcess = RunCommandInShell(details.RunCommand);

            botProcess.StartInfo.WorkingDirectory = details.RootDir;
            ApplyEnvironment(botProcess.StartInfo, details.Environment);
            botProcess.StartInfo.EnvironmentVariables["RLBOT_AGENT_ID"] = details.AgentId;
            botProcess.StartInfo.EnvironmentVariables["RLBOT_SERVER_PORT"] =
                rlbotSocketsPort.ToString();
            botProcess.EnableRaisingEvents = true;

            botProcess.Exited += (_, _) =>
            {
                if (botProcess.ExitCode != 0)
                {
                    Logger.LogError(
                        "Bot {0} exited with error code {1}. See previous logs for more information.",
                        details.Name,
                        botProcess.ExitCode
                    );
                }
            };

            try
            {
                botProcess.Start();
                Logger.LogInformation("Launched bot: {}", details.Name);
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to launch bot {details.Name}: {e.Message}");
            }
        }
    }

    public static void LaunchScripts(
        List<RLBot.Flat.ScriptConfigurationT> scripts,
        int rlbotSocketsPort
    )
    {
        foreach (var script in scripts)
        {
            if (script.RunCommand == "")
            {
                Logger.LogWarning("Script {} must be started manually.", script.Name);
                continue;
            }

            Process scriptProcess = RunCommandInShell(script.RunCommand);

            if (script.RootDir != "")
                scriptProcess.StartInfo.WorkingDirectory = script.RootDir;

            ApplyEnvironment(scriptProcess.StartInfo, script.Environment);
            scriptProcess.StartInfo.EnvironmentVariables["RLBOT_AGENT_ID"] = script.AgentId;
            scriptProcess.StartInfo.EnvironmentVariables["RLBOT_SERVER_PORT"] =
                rlbotSocketsPort.ToString();
            scriptProcess.EnableRaisingEvents = true;

            scriptProcess.Exited += (_, _) =>
            {
                if (scriptProcess.ExitCode != 0)
                {
                    Logger.LogError(
                        "Script {0} exited with error code {1}. See previous logs for more information.",
                        script.Name,
                        scriptProcess.ExitCode
                    );
                }
            };

            try
            {
                scriptProcess.Start();
                Logger.LogInformation("Launched script: {}", script.Name);
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to launch script: {e.Message}");
            }
        }
    }
}
