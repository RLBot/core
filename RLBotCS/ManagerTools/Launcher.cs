using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RLBotCS.MatchManagement
{
    internal class Launcher
    {

        public static void LaunchRocketLeague(int port)
        {


        }

        public static bool IsRocketLeagueRunning(int port)
        {
            // GetProcessByName uses the "friendly name" of the process, no extensions required.
            // Should be OS agnostic (?)
            Process[] candidates = Process.GetProcessesByName("RocketLeague");

            foreach (Process process in candidates)
            {
                // TODO - OS-agnostic check for command line args to see if rocket league was started correctly
            }


            return false;
        }
    }
}
