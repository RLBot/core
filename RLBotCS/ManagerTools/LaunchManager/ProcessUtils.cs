using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
#if WINDOWS
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
#endif

namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
#if WINDOWS
    private static List<string> ParseCommand(string command)
    {
        // Only works on Windows due to exes on Linux running under Wine
        var parts = new List<string>();
        var regex = new Regex(@"(?<match>[""].+?[""]|[^ ]+)");
        var matches = regex.Matches(command);

        foreach (Match match in matches)
        {
            parts.Add(match.Groups["match"].Value.Trim('"'));
        }

        return parts;
    }
#endif

    private static Process RunCommandInShell(string command)
    {
        Process process = new();

#if WINDOWS
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/c {command}";
#else
        process.StartInfo.FileName = "/bin/sh";
        process.StartInfo.Arguments = $"-c \"{command}\"";
#endif

        return process;
    }

    private static void ApplyEnvironment(
        ProcessStartInfo startInfo,
        List<RLBot.Flat.EnvironmentVariableT> environment
    )
    {
        foreach (var variable in environment)
        {
            startInfo.EnvironmentVariables[variable.Name] = variable.Value;
        }
    }

    public static string? GetGameArgs()
    {
        Process[] candidates = Process.GetProcesses();

        foreach (var candidate in candidates)
        {
            if (!candidate.ProcessName.Contains("RocketLeague"))
                continue;

            return GetProcessArgs(candidate);
        }

        return null;
    }

    public static void KillGame()
    {
        Process[] candidates = Process.GetProcesses();

        foreach (var candidate in candidates)
        {
            if (!candidate.ProcessName.Contains("RocketLeague"))
                continue;

            candidate.Kill();
        }
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
#if WINDOWS
        int err = ProcessCommandLine.Retrieve(process, out string commandLine);

        if (err == 0)
            return commandLine;

        Logger.LogError(
            $"Failed to retrieve command line arguments for process {process.ProcessName}: {ProcessCommandLine.ErrorToString(err)}"
        );
        return "";
#else
        // Solution taken from:
        // https://stackoverflow.com/a/58843225/10930209
        return File.ReadAllText($"/proc/{process.Id}/cmdline");
#endif
    }

    public static string? GetRocketLeaguePath()
    {
        // Assumes the game has already been launched
        string? args = GetGameArgs();
        if (args is null)
            return null;

        string directGamePath;

#if WINDOWS
        directGamePath = ParseCommand(args)[0];
#else
        // On Linux, Rocket League is running under Wine so args is something like
        // Z:\home\username\.steam\debian-installation\steamapps\common\rocketleague\Binaries\Win64\RocketLeague.exe-rlbotRLBot_ControllerURL=127.0.0.1:23233RLBot_PacketSendRate=240-nomovie
        // and we must get the real path to RocketLeague.exe from this
        directGamePath = args.Remove(0, 2).Split("-rlbot")[0].Replace("\\", "/");
#endif

        return directGamePath;
    }

    public static bool IsRocketLeagueRunning()
    {
        return Process
            .GetProcesses()
            .Any(candidate => candidate.ProcessName.Contains("RocketLeague"));
    }

    public static bool IsRocketLeagueRunningWithArgs()
    {
        return Process
            .GetProcesses()
            .Any(candidate =>
            {
                if (!candidate.ProcessName.Contains("RocketLeague"))
                    return false;

                var args = GetProcessArgs(candidate);
                return args.Contains("rlbot");
            });
    }
}
