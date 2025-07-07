using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace RLBotCS.ManagerTools;

static class LaunchManager
{
    private const string SteamGameId = "252950";
    public const int RlbotSocketsPort = 23234;
    private const int DefaultGamePort = 50000;
    private const int IdealGamePort = 23233;

    private static readonly ILogger Logger = Logging.GetLogger("LaunchManager");

    public static string? GetGameArgs(bool kill)
    {
        Process[] candidates = Process.GetProcesses();

        foreach (var candidate in candidates)
        {
            if (!candidate.ProcessName.Contains("RocketLeague"))
                continue;

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
#if WINDOWS
        int err = ProcessCommandLine.Retrieve(process, out string commandLine);

        if (err == 0)
            return commandLine;

        Logger.LogError(
            $"Failed to retrieve command line arguments for process {0}: {1}",
            process.ProcessName,
            ProcessCommandLine.ErrorToString(err)
        );
        return "";
#else
        // Solution taken from:
        // https://stackoverflow.com/a/58843225/10930209
        return File.ReadAllText($"/proc/{process.Id}/cmdline");
#endif
    }

    private static string[] GetIdealArgs(int gamePort) =>
        [
            "-rlbot",
            $"RLBot_ControllerURL=127.0.0.1:{gamePort}",
            "RLBot_PacketSendRate=240",
            "-nomovie",
        ];

#if WINDOWS
    private static List<string> ParseCommand(string command)
    {
        // Only works on Windows due to exes on Linux running under Wine
        var parts = new List<string>();
        var regex = new Regex(@"(?<match>[\""].+?[\""]|[^ ]+)");
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

    private static void LaunchGameViaLegendary()
    {
        Process legendary = RunCommandInShell(
            "legendary launch Sugar -rlbot RLBot_ControllerURL=127.0.0.1:23233 RLBot_PacketSendRate=240 -nomovie"
        );
        legendary.Start();
    }

    private static void LaunchGameViaHeroic()
    {
        Process heroic;

#if WINDOWS
        heroic = RunCommandInShell(
            "start \"\" \"heroic://launch?appName=Sugar&runner=legendary&arg=-rlbot&arg=RLBot_ControllerURL%3D127.0.0.1%3A23233&arg=RLBot_PacketSendRate%3D240&arg=-nomovie\""
        );
#else
        heroic = RunCommandInShell(
            "xdg-open 'heroic://launch?appName=Sugar&runner=legendary&arg=-rlbot&arg=RLBot_ControllerURL%3D127.0.0.1%3A23233&arg=RLBot_PacketSendRate%3D240&arg=-nomovie'"
        );
#endif

        heroic.Start();
    }

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

    public static void LaunchRocketLeague(
        RLBot.Flat.Launcher launcherPref,
        string extraArg,
        int gamePort
    )
    {
#if WINDOWS
        switch (launcherPref)
        {
            case RLBot.Flat.Launcher.Steam:
                string steamPath = GetWindowsSteamPath();
                Process steam = new();
                steam.StartInfo.FileName = steamPath;
                steam.StartInfo.Arguments =
                    $"-applaunch {SteamGameId} " + string.Join(" ", GetIdealArgs(gamePort));

                Logger.LogInformation(
                    $"Starting Rocket League with args {steamPath} {steam.StartInfo.Arguments}"
                );
                steam.Start();
                break;
            case RLBot.Flat.Launcher.Epic:
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

                Logger.LogInformation("Finding Rocket League...");
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
                Logger.LogInformation($"Found Rocket League at \"{directGamePath}\"");

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

                Logger.LogInformation($"Starting RocketLeague.exe directly with {idealArgs}");

                // if we don't read the output, the game will hang
                new Thread(() =>
                {
                    epicRocketLeague.StandardOutput.ReadToEnd();
                }).Start();

                break;
            case RLBot.Flat.Launcher.Custom:
                if (extraArg.ToLower() == "legendary")
                {
                    LaunchGameViaLegendary();
                    return;
                }
                else if (extraArg.ToLower() == "heroic")
                {
                    LaunchGameViaHeroic();
                    return;
                }

                throw new NotSupportedException($"Unexpected launcher, \"{extraArg}\"");
            case RLBot.Flat.Launcher.NoLaunch:
                break;
        }
#else
        switch (launcherPref)
        {
            case RLBot.Flat.Launcher.Steam:
                string args = string.Join("%20", GetIdealArgs(gamePort));
                Process rocketLeague = new();
                rocketLeague.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                rocketLeague.StartInfo.FileName = "steam";
                rocketLeague.StartInfo.Arguments = $"steam://rungameid/{SteamGameId}//{args}";

                Logger.LogInformation(
                    $"Starting Rocket League via Steam CLI with {rocketLeague.StartInfo.Arguments}"
                );
                rocketLeague.Start();
                break;
            case RLBot.Flat.Launcher.Epic:
                throw new NotSupportedException(
                    "Epic Games Store is not directly supported on Linux."
                );
            case RLBot.Flat.Launcher.Custom:
                if (extraArg.ToLower() == "legendary")
                {
                    LaunchGameViaLegendary();
                    return;
                }
                else if (extraArg.ToLower() == "heroic")
                {
                    LaunchGameViaHeroic();
                    return;
                }

                throw new NotSupportedException($"Unexpected launcher, \"{extraArg}\"");
            case RLBot.Flat.Launcher.NoLaunch:
                break;
        }
#endif
    }

    public static string? GetRocketLeaguePath()
    {
        // Assumes the game has already been launched
        string? args = GetGameArgs(false);
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
