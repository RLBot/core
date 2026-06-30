using Microsoft.Extensions.Logging;

namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
    private const string SteamGameId = "252950";
    public const int RlbotSocketsPort = 23234;
    private const int DefaultGamePort = 50000;
    private const int IdealGamePort = 23233;

    private static readonly ILogger Logger = Logging.GetLogger("LaunchManager");

    private static string[] GetRLBotArgs(int gamePort) =>
        [
            "-rlbot",
            $"RLBot_ControllerURL=127.0.0.1:{gamePort}",
            "RLBot_PacketSendRate=240",
            "-nomovie",
        ];

    public static void LaunchRocketLeague(
        RLBot.Flat.Launcher launcherPref,
        string extraArg,
        int gamePort
    )
    {
        switch (launcherPref)
        {
            case RLBot.Flat.Launcher.Steam:
                LaunchGameViaSteam(gamePort);
                break;
            case RLBot.Flat.Launcher.Epic:
                LaunchGameViaEpic(gamePort);
                break;
            case RLBot.Flat.Launcher.Custom:
                LaunchCustomLauncher(extraArg, gamePort);
                break;
            case RLBot.Flat.Launcher.NoLaunch:
                break;
        }
    }
}
