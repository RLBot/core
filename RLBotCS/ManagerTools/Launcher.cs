using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using rlbot.flat;

namespace RLBotCS.MatchManagement
{
    internal class Launcher
    {
        public static string steamLaunchArgs =
            "-applaunch "
            + "252950 "
            + "-rlbot "
            + "RLBot_ControllerURL=127.0.0.1:23233 "
            + "RLBot_PacketSendRate=240 "
            + "-nomovie";

        public static void LaunchRocketLeague(rlbot.flat.Launcher launcher)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (launcher)
                {
                    case rlbot.flat.Launcher.Steam:
                        Process rocketLeague = new();
                        string steamPath = GetSteamPath();
                        rocketLeague.StartInfo.FileName = steamPath;
                        rocketLeague.StartInfo.Arguments = steamLaunchArgs;
                        Console.WriteLine($"Starting Rocket League with args {steamPath} {steamLaunchArgs}");
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

        public static string GetSteamPath()
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
