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

    public static string? GetGameArgsAndKill()
    {
        Process[] candidates = Process.GetProcessesByName("RocketLeague");

        foreach (var candidate in candidates)
        {
            string args = GetProcessArgs(candidate);
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
            "-nomovie"
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

    public static void LaunchBots(
        Dictionary<string, List<rlbot.flat.PlayerConfigurationT>> processGroups,
        int rlbotSocketsPort
    )
    {
        foreach (var processGroup in processGroups.Values)
        {
            var mainPlayer = processGroup[0];
            if (mainPlayer.RunCommand == "")
                continue;

            Process botProcess = new();

            if (mainPlayer.Location != "")
                botProcess.StartInfo.WorkingDirectory = mainPlayer.Location;

            try
            {
                var commandParts = ParseCommand(mainPlayer.RunCommand);
                botProcess.StartInfo.FileName = Path.Join(
                    mainPlayer.Location,
                    commandParts[0]
                );
                botProcess.StartInfo.Arguments = string.Join(' ', commandParts.Skip(1));

                List<int> spawnIds = processGroup.Select(player => player.SpawnId).ToList();
                botProcess.StartInfo.EnvironmentVariables["RLBOT_SPAWN_IDS"] = string.Join(
                    ',',
                    spawnIds
                );
                botProcess.StartInfo.EnvironmentVariables["RLBOT_SERVER_PORT"] =
                    rlbotSocketsPort.ToString();

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

            Process scriptProcess = new();

            if (script.Location != "")
                scriptProcess.StartInfo.WorkingDirectory = script.Location;

            try
            {
                var commandParts = ParseCommand(script.RunCommand);
                scriptProcess.StartInfo.FileName = Path.Join(script.Location, commandParts[0]);
                scriptProcess.StartInfo.Arguments = string.Join(' ', commandParts.Skip(1));

                scriptProcess.StartInfo.EnvironmentVariables["RLBOT_SPAWN_IDS"] =
                    script.SpawnId.ToString();
                scriptProcess.StartInfo.EnvironmentVariables["RLBOT_SERVER_PORT"] =
                    rlbotSocketsPort.ToString();

                scriptProcess.Start();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to launch script: {e.Message}");
            }
        }
    }

    public static void LaunchRocketLeague(rlbot.flat.Launcher launcherPref, int gamePort)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            switch (launcherPref)
            {
                case rlbot.flat.Launcher.Steam:
                    string steamPath = GetWindowsSteamPath();
                    Process rocketLeague = new();
                    rocketLeague.StartInfo.FileName = steamPath;
                    rocketLeague.StartInfo.Arguments =
                        $"-applaunch {SteamGameId} "
                        + string.Join(" ", GetIdealArgs(gamePort));

                    Logger.LogInformation(
                        $"Starting Rocket League with args {steamPath} {rocketLeague.StartInfo.Arguments}"
                    );
                    rocketLeague.Start();
                    break;
                case rlbot.flat.Launcher.Epic:
                    // we need a hack to launch the game properly

                    // start the game
                    Process launcher = new();
                    launcher.StartInfo.FileName = "cmd.exe";
                    launcher.StartInfo.Arguments =
                        "/c start \"\" \"com.epicgames.launcher://apps/9773aa1aa54f4f7b80e44bef04986cea%3A530145df28a24424923f5828cc9031a1%3ASugar?action=launch&silent=true\"";
                    launcher.Start();

                    Console.WriteLine("Waiting for Rocket League path details...");

                    // get the game path & login args, the quickly kill the game
                    // todo: add max number of retries
                    string? args = null;
                    while (args is null)
                    {
                        Thread.Sleep(1000);
                        args = GetGameArgsAndKill();
                    }

                    if (args is null)
                        throw new Exception("Failed to get Rocket League args");

                    string gamePath = ParseCommand(args)[0];
                    Logger.LogInformation($"Found Rocket League @ \"{gamePath}\"");

                    // append RLBot args
                    args = args.Replace(gamePath, "");
                    args = args.Replace("\"\"", "");
                    string idealArgs = string.Join(" ", GetIdealArgs(gamePort));
                    // rlbot args need to be first or the game might ignore them :(
                    string modifiedArgs = $"\"{gamePath}\" {idealArgs} {args}";

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
                    throw new NotSupportedException("Unexpected launcher. Use Steam.");
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
                    throw new NotSupportedException("Epic Games not supported on Linux.");
                case rlbot.flat.Launcher.Custom:
                    throw new NotSupportedException("Unexpected launcher. Use Steam.");
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
