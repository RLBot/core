using System.Diagnostics;

namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
    private static void LaunchGameViaLegendary(int gamePort)
    {
        string args = string.Join(" ", GetRLBotArgs(gamePort));
        Process legendary = RunCommandInShell($"legendary launch Sugar {args} -noeac");
        legendary.Start();
    }

    private static void LaunchGameViaHeroic(int gamePort)
    {
        string[] rlbotArgs = GetRLBotArgs(gamePort);
        string heroicArgs = string.Join(
            "",
            rlbotArgs.Select(a => $"&arg={Uri.EscapeDataString(a)}")
        );
        string heroicUrl =
            $"heroic://launch?appName=Sugar&runner=legendary{heroicArgs}&arg=-noeac";

        Process heroic;

#if WINDOWS
        heroic = RunCommandInShell($"start \"\" \"{heroicUrl}\"");
#else
        heroic = RunCommandInShell($"xdg-open '{heroicUrl}'");
#endif

        heroic.Start();
    }

    private static void LaunchCustomLauncher(string extraArg, int gamePort)
    {
        if (extraArg.Equals("legendary", StringComparison.OrdinalIgnoreCase))
        {
            LaunchGameViaLegendary(gamePort);
        }
        else if (extraArg.Equals("heroic", StringComparison.OrdinalIgnoreCase))
        {
            LaunchGameViaHeroic(gamePort);
        }
        else
        {
            throw new NotSupportedException($"Unexpected launcher, \"{extraArg}\"");
        }
    }
}
