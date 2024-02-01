using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RLBotCS.MatchManagement
{
    internal class Launcher
    {
        public static string steamGameID = "252950";

        public static string[] GetIdealArgs(int gamePort)
        {
            return ["-rlbot", $"RLBot_ControllerURL=127.0.0.1:{gamePort}", "RLBot_PacketSendRate=240", "-nomovie"];
        }

        public static void LaunchBots(List<rlbot.flat.PlayerConfigurationT> players)
        {
            foreach (var player in players)
            {
                if (player.RunCommand == "")
                {
                    continue;
                }

                Process botProcess = new();

                if (player.Location != "")
                {
                    botProcess.StartInfo.WorkingDirectory = player.Location;
                }

                string[] command = player.RunCommand.Split(' ');
                botProcess.StartInfo.FileName = command[0];
                botProcess.StartInfo.Arguments = string.Join(" ", command[1..]);
                botProcess.Start();
            }
        }

        public static void LaunchScripts(List<rlbot.flat.ScriptConfigurationT> scripts)
        {
            foreach (var script in scripts)
            {
                if (script.RunCommand == "")
                {
                    continue;
                }

                Process scriptProcess = new();

                if (script.Location != "")
                {
                    scriptProcess.StartInfo.WorkingDirectory = script.Location;
                }

                string[] command = script.RunCommand.Split(' ');
                scriptProcess.StartInfo.FileName = command[0];
                scriptProcess.StartInfo.Arguments = string.Join(" ", command[1..]);
                scriptProcess.Start();
            }
        }

        public static void LaunchRocketLeague(rlbot.flat.Launcher launcher, int gamePort)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (launcher)
                {
                    case rlbot.flat.Launcher.Steam:
                        Process rocketLeague = new();

                        string steamPath = GetSteamPath();
                        rocketLeague.StartInfo.FileName = steamPath;
                        rocketLeague.StartInfo.Arguments =
                            $"-applaunch {steamGameID} " + string.Join(" ", GetIdealArgs(gamePort));

                        Console.WriteLine(
                            $"Starting Rocket League with args {steamPath} {rocketLeague.StartInfo.Arguments}"
                        );
                        rocketLeague.Start();
                        break;
                    case rlbot.flat.Launcher.Epic:
                        break;
                    case rlbot.flat.Launcher.Custom:
                        break;
                    default:
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (launcher)
                {
                    case rlbot.flat.Launcher.Steam:
                        Process rocketLeague = new();
                        rocketLeague.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                        string args = string.Join("%20", GetIdealArgs(gamePort));
                        rocketLeague.StartInfo.FileName = "steam";
                        rocketLeague.StartInfo.Arguments = $"steam://rungameid/{steamGameID}//{args}";

                        Console.WriteLine(
                            $"Starting Rocket League via Steam CLI with {rocketLeague.StartInfo.Arguments}"
                        );
                        rocketLeague.Start();
                        break;
                    case rlbot.flat.Launcher.Epic:
                        break;
                    case rlbot.flat.Launcher.Custom:
                        break;
                    default:
                        break;
                }
            }
        }

        public static bool IsRocketLeagueRunning()
        {
            // GetProcessByName uses the "friendly name" of the process, no extensions required.
            // Should be OS agnostic (?)
            Process[] candidates = Process.GetProcessesByName("RocketLeague");

            if (candidates.Length > 0)
            {
                return true;
            }
            return false;
        }

        private static string GetSteamPath()
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key != null)
            {
                return key.GetValue("SteamExe").ToString();
            }
            else
            {
                throw new FileNotFoundException(
                    "Could not find registry entry for SteamExe... Is Steam installed?"
                );
            }
        }
    }
}
