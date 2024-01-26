using System.Diagnostics;
using rlbot.flat;

namespace RLBotCS.MatchManagement
{
    internal class Launcher
    {
        public static void LaunchRocketLeague(int port, rlbot.flat.Launcher launcher)
        {
            switch (launcher)
            {
                case rlbot.flat.Launcher.Steam:
                    break;
                case rlbot.flat.Launcher.Epic:
                    break;
                case rlbot.flat.Launcher.Custom:
                    break;
                default:
                    break;
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
    }
}
