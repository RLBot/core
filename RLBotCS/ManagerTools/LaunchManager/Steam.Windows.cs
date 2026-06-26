#if WINDOWS
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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

    private static bool IsValidSteamRoot(string path)
    {
        return Directory.Exists(Path.Combine(path, "steamapps"));
    }

    private static List<string> GetWindowsSteamLibraryFolders()
    {
        List<string> folders = [];

        string steamPath = GetWindowsSteamPath();
        string? steamRoot = Path.GetDirectoryName(steamPath);
        if (steamRoot != null && IsValidSteamRoot(steamRoot) && !folders.Contains(steamRoot))
            folders.Add(steamRoot);

        string vdfPath = Path.Combine(steamRoot ?? "", "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdfPath))
        {
            try
            {
                string vdf = File.ReadAllText(vdfPath);
                var matches = Regex.Matches(vdf, @"""path""\s+""([^""]+)""");
                foreach (Match match in matches)
                {
                    string path = match.Groups[1].Value;
                    if (Directory.Exists(path) && !folders.Contains(path))
                        folders.Add(path);
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to read Steam libraryfolders.vdf: {e.Message}");
            }
        }

        return folders;
    }

    private static string? FindWindowsRocketLeaguePath()
    {
        foreach (var folder in GetWindowsSteamLibraryFolders())
        {
            string manifestPath = Path.Combine(
                folder,
                "steamapps",
                $"appmanifest_{SteamGameId}.acf"
            );
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                string manifest = File.ReadAllText(manifestPath);
                var match = Regex.Match(manifest, @"""installdir""\s+""([^""]+)""");
                if (!match.Success)
                    continue;

                string installDir = match.Groups[1].Value;
                string exePath = Path.Combine(
                    folder,
                    "steamapps",
                    "common",
                    installDir,
                    "Binaries",
                    "Win64",
                    "RocketLeague.exe"
                );
                if (File.Exists(exePath))
                    return exePath;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to read Rocket League appmanifest: {e.Message}");
            }
        }

        return null;
    }

    private static void LaunchGameViaSteam(int gamePort)
    {
        string? gamePath = FindWindowsRocketLeaguePath();
        if (gamePath == null)
            throw new FileNotFoundException(
                "Could not find Rocket League installation. Ensure Rocket League is installed via Steam."
            );

        string args = string.Join(" ", GetRLBotArgs(gamePort));

        Process rocketLeague = new();
        rocketLeague.StartInfo.FileName = gamePath;

        foreach (var arg in GetRLBotArgs(gamePort))
            rocketLeague.StartInfo.ArgumentList.Add(arg);

        rocketLeague.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        // Let Steamworks initialize by providing the AppId,
        // so the game can authenticate with the running Steam client.
        rocketLeague.StartInfo.Environment["SteamAppId"] = SteamGameId;
        rocketLeague.StartInfo.Environment["SteamGameId"] = SteamGameId;

        Logger.LogInformation($"Starting Rocket League without EAC: {gamePath} {args}");
        rocketLeague.Start();
    }
}
#endif
