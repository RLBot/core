using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WmiLight;

namespace RLBotCS.ManagerTools;

internal static class LaunchManager
{
    private const string SteamGameId = "252950";
    public const int RlbotSocketsPort = 23234;
    private const int DefaultGamePort = 50000;
    private const int IdealGamePort = 23233;

    private static readonly ILogger Logger = Logging.GetLogger("LaunchManager");

    public static string? GetGameArgs(bool kill)
    {
        Process[] candidates = Process.GetProcessesByName("RocketLeague");

        foreach (var candidate in candidates)
        {
            string args = GetProcessArgs(candidate);
            if (kill)
                candidate.Kill();

            return args;
        }

        return null;
    }

    public static int FindUsableGamePort(int rlbotSocketsPort)
    {
        Process[] candidates = Process.GetProcessesByName("RocketLeague");

        // Search cmd line args for port
        foreach (var candidate in candidates)
        {
            string[] args = GetProcessArgs(candidate).Split(" ");
            foreach (var arg in args)
                if (arg.Contains("RLBot_ControllerURL"))
                {
                    string[] parts = arg.Split(':');
                    var port = parts[^1].TrimEnd('"');
                    return int.Parse(port);
                }
        }

        for (int portToTest = IdealGamePort; portToTest < 65535; portToTest++)
        {
            if (portToTest == rlbotSocketsPort)
                // Skip the port we're using for sockets
                continue;

            // Try booting up a server on the port
            try
            {
                TcpListener listener = new(IPAddress.Any, portToTest);
                listener.Start();
                listener.Stop();
                return portToTest;
            }
            catch (SocketException) { }
        }

        return DefaultGamePort;
    }

    private static string GetProcessArgs(Process process)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return process.StartInfo.Arguments;

        using WmiConnection con = new WmiConnection();
        WmiQuery objects = con.CreateQuery(
            $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"
        );
        return objects.SingleOrDefault()?["CommandLine"]?.ToString() ?? "";
    }

    private static string[] GetIdealArgs(int gamePort) =>
        [
            "-rlbot",
            $"RLBot_ControllerURL=127.0.0.1:{gamePort}",
            "RLBot_PacketSendRate=240",
            "-nomovie",
        ];

    private static List<string> ParseCommand(string command)
    {
        var parts = new List<string>();
        var regex = new Regex(@"(?<match>[\""].+?[\""]|[^ ]+)");
        var matches = regex.Matches(command);

        foreach (Match match in matches)
        {
            parts.Add(match.Groups["match"].Value.Trim('"'));
        }

        return parts;
    }

    private static Process RunCommandInShell(string command)
    {
        Process process = new();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            process.StartInfo.FileName = "/bin/sh";
            process.StartInfo.Arguments = $"-c \"{command}\"";
        }
        else
            throw new PlatformNotSupportedException(
                "RLBot is not supported on non-Windows/Linux platforms"
            );

        return process;
    }

    private static void LaunchGameViaLegendary()
    {
        Process legendary = RunCommandInShell(
            "legendary launch Sugar -rlbot RLBot_ControllerURL=127.0.0.1:23233 RLBot_PacketSendRate=240 -nomovie"
        );
        legendary.Start();
    }

    public static void LaunchBots(
        Dictionary<string, rlbot.flat.PlayerConfigurationT> processGroups,
        int rlbotSocketsPort
    )
    {
        foreach (var mainPlayer in processGroups.Values)
        {
            if (mainPlayer.RunCommand == "")
                continue;

            Process botProcess = RunCommandInShell(mainPlayer.RunCommand);

            if (mainPlayer.RootDir != "")
                botProcess.StartInfo.WorkingDirectory = mainPlayer.RootDir;

            botProcess.StartInfo.EnvironmentVariables["RLBOT_AGENT_ID"] = mainPlayer.AgentId;
            botProcess.StartInfo.EnvironmentVariables["RLBOT_SERVER_PORT"] =
                rlbotSocketsPort.ToString();

            try
            {
                botProcess.Start();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to launch bot {mainPlayer.Name}: {e.Message}");
            }
        }
    }

    public static void LaunchScripts(
        List<rlbot.flat.ScriptConfigurationT> scripts,
        int rlbotSocketsPort
    )
    {
        foreach (var script in scripts)
        {
            if (script.RunCommand == "")
                continue;

            Process scriptProcess = RunCommandInShell(script.RunCommand);

            if (script.RootDir != "")
                scriptProcess.StartInfo.WorkingDirectory = script.RootDir;

            scriptProcess.StartInfo.EnvironmentVariables["RLBOT_AGENT_ID"] = script.AgentId;
            scriptProcess.StartInfo.EnvironmentVariables["RLBOT_SERVER_PORT"] =
                rlbotSocketsPort.ToString();

            try
            {
                scriptProcess.Start();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to launch script: {e.Message}");
            }
        }
    }

    public static void LaunchRocketLeague(
        rlbot.flat.Launcher launcherPref,
        string extraArg,
        int gamePort
    )
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            switch (launcherPref)
            {
                case rlbot.flat.Launcher.Steam:
                    string steamPath = GetWindowsSteamPath();
                    Process steam = new();
                    steam.StartInfo.FileName = steamPath;
                    steam.StartInfo.Arguments =
                        $"-applaunch {SteamGameId} "
                        + string.Join(" ", GetIdealArgs(gamePort));

                    Logger.LogInformation(
                        $"Starting Rocket League with args {steamPath} {steam.StartInfo.Arguments}"
                    );
                    steam.Start();
                    break;
                case rlbot.flat.Launcher.Epic:
                    bool nonRLBotGameRunning = IsRocketLeagueRunning();

                    // we don't need to start the game because there's another instance of non-rlbot rocket league open
                    if (!nonRLBotGameRunning)
                    {
                        // we need a hack to launch the game properly
                        // start the game
                        Process launcher = new();
                        launcher.StartInfo.FileName = "cmd.exe";
                        launcher.StartInfo.Arguments =
                            "/c start \"\" \"com.epicgames.launcher://apps/9773aa1aa54f4f7b80e44bef04986cea%3A530145df28a24424923f5828cc9031a1%3ASugar?action=launch&silent=true\"";
                        launcher.Start();

                        // wait for it to start
                        Thread.Sleep(1000);
                    }

                    Console.WriteLine("Waiting for Rocket League path details...");
                    string? args = null;

                    // get the game path & login args, the quickly kill the game
                    // todo: add max number of retries
                    while (args is null)
                    {
                        // don't kill the game if it was already running, and not for RLBot
                        args = GetGameArgs(!nonRLBotGameRunning);
                        Thread.Sleep(1000);
                    }

                    if (args is null)
                        throw new Exception("Failed to get Rocket League args");

                    string directGamePath = ParseCommand(args)[0];
                    Logger.LogInformation($"Found Rocket League @ \"{directGamePath}\"");

                    // append RLBot args
                    args = args.Replace(directGamePath, "");
                    args = args.Replace("\"\"", "");
                    string idealArgs = string.Join(" ", GetIdealArgs(gamePort));
                    // rlbot args need to be first or the game might ignore them :(
                    string modifiedArgs = $"\"{directGamePath}\" {idealArgs} {args}";

                    // wait for the game to fully close
                    while (IsRocketLeagueRunning())
                        Thread.Sleep(500);

                    // relaunch the game with the new args
                    Process epicRocketLeague = new();
                    epicRocketLeague.StartInfo.FileName = "cmd.exe";
                    epicRocketLeague.StartInfo.Arguments = $"/c \"{modifiedArgs}\"";

                    // prevent the game from printing to the console
                    epicRocketLeague.StartInfo.UseShellExecute = false;
                    epicRocketLeague.StartInfo.RedirectStandardOutput = true;
                    epicRocketLeague.StartInfo.RedirectStandardError = true;
                    epicRocketLeague.Start();

                    Logger.LogInformation(
                        $"Starting RocketLeague.exe directly with {idealArgs}"
                    );

                    // if we don't read the output, the game will hang
                    new Thread(() =>
                    {
                        epicRocketLeague.StandardOutput.ReadToEnd();
                    }).Start();

                    break;
                case rlbot.flat.Launcher.Custom:
                    if (extraArg.ToLower() == "legendary")
                    {
                        LaunchGameViaLegendary();
                        return;
                    }

                    throw new NotSupportedException($"Unexpected launcher, \"{extraArg}\"");
                case rlbot.flat.Launcher.NoLaunch:
                    break;
            }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            switch (launcherPref)
            {
                case rlbot.flat.Launcher.Steam:
                    string args = string.Join("%20", GetIdealArgs(gamePort));
                    Process rocketLeague = new();
                    rocketLeague.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    rocketLeague.StartInfo.FileName = "steam";
                    rocketLeague.StartInfo.Arguments =
                        $"steam://rungameid/{SteamGameId}//{args}";

                    Logger.LogInformation(
                        $"Starting Rocket League via Steam CLI with {rocketLeague.StartInfo.Arguments}"
                    );
                    rocketLeague.Start();
                    break;
                case rlbot.flat.Launcher.Epic:
                    throw new NotSupportedException(
                        "Epic Games Store is not directly supported on Linux."
                    );
                case rlbot.flat.Launcher.Custom:
                    if (extraArg.ToLower() == "legendary")
                    {
                        LaunchGameViaLegendary();
                        return;
                    }

                    throw new NotSupportedException($"Unexpected launcher, \"{extraArg}\"");
                case rlbot.flat.Launcher.NoLaunch:
                    break;
            }
        else
            throw new PlatformNotSupportedException(
                "RLBot is not supported on non-Windows/Linux platforms"
            );
    }

    public static bool IsRocketLeagueRunning() =>
        Process
            .GetProcesses()
            .Any(candidate => candidate.ProcessName.Contains("RocketLeague"));

    public static bool IsRocketLeagueRunningWithArgs()
    {
        Process[] candidates = Process.GetProcesses();

        foreach (var candidate in candidates)
        {
            if (!candidate.ProcessName.Contains("RocketLeague"))
                continue;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true;

            var args = GetProcessArgs(candidate);
            if (args.Contains("rlbot"))
                return true;
        }

        return false;
    }

    private static string GetWindowsSteamPath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException(
                "Getting Windows path on non-Windows platform"
            );

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if (key?.GetValue("SteamExe")?.ToString() is { } value)
            return value;

        throw new FileNotFoundException(
            "Could not find registry entry for SteamExe. Is Steam installed?"
        );
    }
}
