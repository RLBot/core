using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

    public static int FindUsableGamePort()
    {
        Process[] candidates = Process.GetProcessesByName("RocketLeague");

        // Search cmd line args for port
        foreach (var candidate in candidates)
        {
            string[] args = GetProcessArgs(candidate);
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
            if (portToTest == RlbotSocketsPort)
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

    private static string[] GetProcessArgs(Process process)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return process.StartInfo.Arguments.Split(' ');

        using WmiConnection con = new WmiConnection();
        WmiQuery objects = con.CreateQuery(
            $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"
        );
        return objects.SingleOrDefault()?["CommandLine"]?.ToString()?.Split(" ") ?? [];
    }

    private static string[] GetIdealArgs(int gamePort) =>
        [
            "-rlbot",
            $"RLBot_ControllerURL=127.0.0.1:{gamePort}",
            "RLBot_PacketSendRate=240",
            "-nomovie"
        ];

    public static void LaunchBots(
        List<rlbot.flat.PlayerConfigurationT> players,
        int rlbotSocketsPort
    )
    {
        foreach (var player in players)
        {
            if (player.RunCommand == "")
                continue;

            Process botProcess = new();

            if (player.Location != "")
                botProcess.StartInfo.WorkingDirectory = player.Location;

            try
            {
                string[] commandParts = player.RunCommand.Split(' ', 2);
                botProcess.StartInfo.FileName = Path.Join(player.Location, commandParts[0]);
                botProcess.StartInfo.Arguments = commandParts[1];

                botProcess.StartInfo.EnvironmentVariables["BOT_SPAWN_ID"] =
                    player.SpawnId.ToString();
                botProcess.StartInfo.EnvironmentVariables["RLBOT_SERVER_PORT"] =
                    rlbotSocketsPort.ToString();

                botProcess.Start();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to launch bot {player.Name}: {e.Message}");
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
                string[] commandParts = script.RunCommand.Split(' ', 2);
                scriptProcess.StartInfo.FileName = Path.Join(script.Location, commandParts[0]);
                scriptProcess.StartInfo.Arguments = commandParts[1];

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

    public static void LaunchRocketLeague(rlbot.flat.Launcher launcher, int gamePort)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            switch (launcher)
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
                    throw new NotSupportedException("Epic Games not supported.");
                case rlbot.flat.Launcher.Custom:
                    throw new NotSupportedException("Unexpected launcher. Use Steam.");
            }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            switch (launcher)
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
                    throw new NotSupportedException("Epic Games not supported.");
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
