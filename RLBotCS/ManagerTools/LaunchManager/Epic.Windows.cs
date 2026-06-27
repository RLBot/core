#if WINDOWS
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
    private static void LaunchGameViaEpic(int gamePort)
    {
        if (!IsRocketLeagueRunning())
        {
            // Start with a fresh Launch.log so we don't read stale data from a previous run.
            ReadLog.DeleteLog();

            // To launch RocketLeague for Epic we need some extra login parameters from Epic.
            // We get these by launching the game normally, reading the args, and then closing it again.

            Process launcher = new();
            launcher.StartInfo.FileName = "cmd.exe";
            launcher.StartInfo.Arguments =
                "/c start \"\" \"com.epicgames.launcher://apps/9773aa1aa54f4f7b80e44bef04986cea%3A530145df28a24424923f5828cc9031a1%3ASugar?action=launch&silent=true\"";
            launcher.Start();
            Thread.Sleep(500);
        } else {
            Logger.LogInformation("Relaunching Rocket League via Epic.");
        }

        // Get the game path and login info from launch logs
        (string, string)? pathAndAuth = null;
        while (pathAndAuth is null)
        {
            pathAndAuth = ReadLog.GetGamePathAndAuth();
            Thread.Sleep(500);
        }

        KillGame();
        string directGamePath = pathAndAuth.Value.Item1;
        string authArgs = pathAndAuth.Value.Item2;
        Logger.LogDebug($"Epic RocketLeague args: {authArgs}");
        Logger.LogInformation($"Found Rocket League at \"{directGamePath}\"");

        // Wait for the game to fully close
        Logger.LogDebug("Waiting for Rocket League to fully close...");
        while (IsRocketLeagueRunning())
            Thread.Sleep(500);

        string rlbotArgs = string.Join(" ", GetRLBotArgs(gamePort));
        string modifiedArgs = $"{rlbotArgs} {authArgs}";

        // Relaunch the game with the new args
        Process epicRocketLeague = new();
        epicRocketLeague.StartInfo.FileName = directGamePath;
        epicRocketLeague.StartInfo.Arguments = modifiedArgs;

        // Prevent the game from printing to the console
        epicRocketLeague.StartInfo.UseShellExecute = false;
        epicRocketLeague.StartInfo.RedirectStandardOutput = true;
        epicRocketLeague.StartInfo.RedirectStandardError = true;

        Logger.LogInformation($"Starting Rocket League with Epic: {rlbotArgs}");
        Logger.LogDebug(
            $"Full command: {epicRocketLeague.StartInfo.FileName} {epicRocketLeague.StartInfo.Arguments}"
        );
        epicRocketLeague.Start();

        // If we don't read the output, the game will hang
        new Thread(() =>
        {
            epicRocketLeague.StandardOutput.ReadToEnd();
        }).Start();
    }
}
#endif
