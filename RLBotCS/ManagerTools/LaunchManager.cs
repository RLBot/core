using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RLBotCS.ManagerTools;

internal static class LaunchManager
{
    private const string SteamGameId = "252950";
    public const int RlbotSocketsPort = 23234;
    private const int DefaultGamePort = 50000;
    private const int IdealGamePort = 23233;

    public static int FindUsableGamePort()
    {
        Process[] candidates = Process.GetProcessesByName("RocketLeague");

        // Search cmd line args for port
        foreach (var candidate in candidates)
        {
            string[] args = candidate.StartInfo.Arguments.Split(' ');
            foreach (var arg in args)
                if (arg.Contains("RLBot_ControllerURL"))
                {
                    string[] parts = arg.Split(':');
                    return int.Parse(parts[^1]);
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

    private static string[] GetIdealArgs(int gamePort) =>
        ["-rlbot", $"RLBot_ControllerURL=127.0.0.1:{gamePort}", "RLBot_PacketSendRate=240", "-nomovie"];

    public static void LaunchBots(List<rlbot.flat.PlayerConfigurationT> players)
    {
        foreach (var player in players)
        {
            if (player.RunCommand == "")
                continue;

            Process botProcess = new();

            if (player.Location != "")
                botProcess.StartInfo.WorkingDirectory = player.Location;

            string[] command = player.RunCommand.Split(' ');
            botProcess.StartInfo.FileName = command[0];
            botProcess.StartInfo.Arguments = string.Join(" ", command[1..]);

            botProcess.StartInfo.EnvironmentVariables["BOT_SPAWN_ID"] = player.SpawnId.ToString();

            botProcess.Start();
        }
    }

    public static void LaunchScripts(List<rlbot.flat.ScriptConfigurationT> scripts)
    {
        foreach (var script in scripts)
        {
            if (script.RunCommand == "")
                continue;

            Process scriptProcess = new();

            if (script.Location != "")
                scriptProcess.StartInfo.WorkingDirectory = script.Location;

            string[] command = script.RunCommand.Split(' ');
            scriptProcess.StartInfo.FileName = command[0];
            scriptProcess.StartInfo.Arguments = string.Join(" ", command[1..]);
            scriptProcess.Start();
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
                        $"-applaunch {SteamGameId} " + string.Join(" ", GetIdealArgs(gamePort));

                    Console.WriteLine(
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
                    rocketLeague.StartInfo.Arguments = $"steam://rungameid/{SteamGameId}//{args}";

                    Console.WriteLine(
                        $"Starting Rocket League via Steam CLI with {rocketLeague.StartInfo.Arguments}"
                    );
                    rocketLeague.Start();
                    break;
                case rlbot.flat.Launcher.Epic:
                    throw new NotSupportedException("Epic Games not supported.");
                case rlbot.flat.Launcher.Custom:
                    throw new NotSupportedException("Unexpected launcher. Use Steam.");
            }
    }

    public static bool IsRocketLeagueRunning() => Process.GetProcesses()
        .Any(candidate => candidate.ProcessName.Contains("RocketLeague"));

    private static string GetWindowsSteamPath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Getting Windows path on non-Windows platform");

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if (key?.GetValue("SteamExe")?.ToString() is { } value)
            return value;

        throw new FileNotFoundException(
            "Could not find registry entry for SteamExe. Is Steam installed?"
        );
    }
}
