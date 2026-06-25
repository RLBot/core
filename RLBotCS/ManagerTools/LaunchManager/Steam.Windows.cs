#if WINDOWS
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
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

    private static void LaunchGameViaSteam(int gamePort)
    {
        string steamPath = GetWindowsSteamPath();
        Process steam = new();
        steam.StartInfo.FileName = steamPath;
        steam.StartInfo.Arguments =
            $"-applaunch {SteamGameId} " + string.Join(" ", GetRLBotArgs(gamePort));

        Logger.LogInformation(
            $"Starting Rocket League with steam: {steamPath} {steam.StartInfo.Arguments}"
        );
        steam.Start();
    }
}
#endif
