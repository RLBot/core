using System.Diagnostics;

namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
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

    private static void LaunchCustomLauncher(string extraArg)
    {
        if (extraArg.Equals("legendary", StringComparison.OrdinalIgnoreCase))
        {
            LaunchGameViaLegendary();
        }
        else if (extraArg.Equals("heroic", StringComparison.OrdinalIgnoreCase))
        {
            LaunchGameViaHeroic();
        }
        else
        {
            throw new NotSupportedException($"Unexpected launcher, \"{extraArg}\"");
        }
    }
}
