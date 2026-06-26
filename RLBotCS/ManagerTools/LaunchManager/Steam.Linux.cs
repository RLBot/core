#if !WINDOWS
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
    private static string? ResolveDirectorySymlink(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return null;

            var info = new DirectoryInfo(path);
            var resolved = info.LinkTarget != null ? info.ResolveLinkTarget(true) : info;
            return resolved?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValidSteamRoot(string path)
    {
        return Directory.Exists(Path.Combine(path, "steamapps"));
    }

    private static void AddSteamRoot(string? path, List<string> roots)
    {
        if (!string.IsNullOrEmpty(path) && IsValidSteamRoot(path) && !roots.Contains(path))
        {
            roots.Add(path);
        }
    }

    private static List<string> GetLinuxSteamRoots()
    {
        List<string> roots = [];
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Steam creates these symlinks pointing to the actual installation.
        // Resolving them is the most reliable way to find the real root.
        AddSteamRoot(ResolveDirectorySymlink(Path.Combine(home, ".steam", "steam")), roots);
        AddSteamRoot(ResolveDirectorySymlink(Path.Combine(home, ".steam", "root")), roots);

        // Standard XDG data location.
        string xdgDataHome =
            Environment.GetEnvironmentVariable("XDG_DATA_HOME")
            ?? Path.Combine(home, ".local", "share");
        AddSteamRoot(Path.Combine(xdgDataHome, "Steam"), roots);

        // Sandbox/ alternative distribution paths.
        AddSteamRoot(
            Path.Combine(
                home,
                ".var",
                "app",
                "com.valvesoftware.Steam",
                ".local",
                "share",
                "Steam"
            ),
            roots
        );
        AddSteamRoot(
            Path.Combine(home, "snap", "steam", "common", ".local", "share", "Steam"),
            roots
        );

        // Last resort: locate the steam binary in PATH and derive the root from it.
        AddSteamRoot(FindSteamRootFromPath(), roots);

        return roots;
    }

    private static string? FindSteamRootFromPath()
    {
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        foreach (var dir in pathEnv.Split(':'))
        {
            string steamExe = Path.Combine(dir, "steam");
            if (!File.Exists(steamExe))
                continue;

            try
            {
                var info = new FileInfo(steamExe);
                var resolved = info.LinkTarget != null ? info.ResolveLinkTarget(true) : info;
                string? target = resolved?.FullName;
                if (target == null)
                    continue;

                // The binary is usually at <root>/bin/steam or <root>/steam.
                DirectoryInfo? parent = Directory.GetParent(target);
                if (parent?.Name == "bin")
                    parent = parent.Parent;

                if (parent != null && IsValidSteamRoot(parent.FullName))
                    return parent.FullName;
            }
            catch
            {
                // Ignore individual PATH entries that cannot be inspected.
            }
        }

        return null;
    }

    private static List<string> GetLinuxSteamLibraryFolders()
    {
        var folders = new List<string>();

        foreach (var steamRoot in GetLinuxSteamRoots())
        {
            if (!folders.Contains(steamRoot))
                folders.Add(steamRoot);

            string vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdfPath))
                continue;

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

    private static string? FindLinuxRocketLeaguePath()
    {
        foreach (var folder in GetLinuxSteamLibraryFolders())
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

    private static Version? GetProtonVersion(string protonDir)
    {
        string name = Path.GetFileName(protonDir);
        var match = Regex.Match(name, @"Proton\s+(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
        if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
            return version;
        return null;
    }

    private static string? GetConfiguredProtonToolName()
    {
        foreach (var steamRoot in GetLinuxSteamRoots())
        {
            string configPath = Path.Combine(steamRoot, "config", "config.vdf");
            if (!File.Exists(configPath))
                continue;

            try
            {
                string config = File.ReadAllText(configPath);
                var match = Regex.Match(
                    config,
                    @"""CompatToolMapping""[\s\S]*?""252950""\s*\{\s*""name""\s*""([^""]+)""",
                    RegexOptions.Singleline
                );
                if (match.Success)
                {
                    string toolName = match.Groups[1].Value;
                    Logger.LogInformation(
                        $"Steam compatibility tool for Rocket League: {toolName}"
                    );
                    return toolName;
                }

                Logger.LogInformation(
                    "No compatibility tool entry found in Steam config for Rocket League."
                );
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to read Steam config.vdf: {e.Message}");
            }
        }

        return null;
    }

    private static string? GetProtonToolNameFromManifest(string protonDir)
    {
        string manifestPath = Path.Combine(protonDir, "toolmanifest.vdf");
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            string manifest = File.ReadAllText(manifestPath);
            var match = Regex.Match(manifest, @"""nameid""\s*""([^""]+)""");
            if (match.Success)
                return match.Groups[1].Value;

            match = Regex.Match(manifest, @"""name""\s*""([^""]+)""");
            if (match.Success)
                return match.Groups[1].Value;
        }
        catch (Exception e)
        {
            Logger.LogWarning($"Failed to read tool manifest {protonDir}: {e.Message}");
        }

        return null;
    }

    private static string NormalizeProtonName(string name)
    {
        return Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "");
    }

    private static List<int>? ExtractProtonVersion(string name)
    {
        var matches = Regex.Matches(name, @"\d+");
        if (matches.Count == 0)
            return null;
        return matches.Select(m => int.Parse(m.Value)).ToList();
    }

    private static bool ToolNameMatchesDirectory(string toolName, string protonDir)
    {
        string? manifestName = GetProtonToolNameFromManifest(protonDir);
        if (
            manifestName != null
            && manifestName.Equals(toolName, StringComparison.OrdinalIgnoreCase)
        )
            return true;

        string dirName = Path.GetFileName(protonDir);

        // Try a normalized match, e.g. "proton_10_4" == "Proton 10.4".
        if (NormalizeProtonName(toolName) == NormalizeProtonName(dirName))
            return true;

        // Try matching the numeric version components (common prefix).
        var toolVersion = ExtractProtonVersion(toolName);
        var dirVersion = ExtractProtonVersion(dirName);
        if (toolVersion != null && dirVersion != null)
        {
            int commonLength = Math.Min(toolVersion.Count, dirVersion.Count);
            if (toolVersion.Take(commonLength).SequenceEqual(dirVersion.Take(commonLength)))
                return true;
        }

        // Special names like Hotfix / Experimental.
        if (
            toolName.Contains("hotfix", StringComparison.OrdinalIgnoreCase)
            && dirName.Contains("Hotfix", StringComparison.OrdinalIgnoreCase)
        )
            return true;

        if (
            toolName.Contains("experimental", StringComparison.OrdinalIgnoreCase)
            && dirName.Contains("Experimental", StringComparison.OrdinalIgnoreCase)
        )
            return true;

        return false;
    }

    private static string? FindLinuxProtonPath()
    {
        string? configuredTool = GetConfiguredProtonToolName();

        string? newestProtonPath = null;
        Version? newestVersion = null;
        string? anyProtonPath = null;

        foreach (var folder in GetLinuxSteamLibraryFolders())
        {
            string commonPath = Path.Combine(folder, "steamapps", "common");
            if (!Directory.Exists(commonPath))
                continue;

            foreach (var protonDir in Directory.GetDirectories(commonPath, "Proton*"))
            {
                string protonExe = Path.Combine(protonDir, "proton");
                if (!File.Exists(protonExe))
                    continue;

                anyProtonPath ??= protonExe;

                // Prefer the Proton version Steam has configured for Rocket League.
                if (
                    configuredTool != null
                    && ToolNameMatchesDirectory(configuredTool, protonDir)
                )
                {
                    Logger.LogInformation($"Using configured Proton: {protonDir}");
                    return protonExe;
                }

                // Track the newest numeric Proton as a fallback.
                Version? version = GetProtonVersion(protonDir);
                if (version != null && (newestVersion == null || version > newestVersion))
                {
                    newestVersion = version;
                    newestProtonPath = protonExe;
                }
            }
        }

        string? fallbackPath = newestProtonPath ?? anyProtonPath;
        if (fallbackPath != null)
        {
            Logger.LogInformation($"Falling back to installed Proton: {fallbackPath}");
            return fallbackPath;
        }

        return null;
    }

    private static void LaunchGameViaSteam(int gamePort)
    {
        string? gamePath = FindLinuxRocketLeaguePath();
        if (gamePath == null)
            throw new FileNotFoundException(
                "Could not find Rocket League installation. Ensure Rocket League is installed via Steam."
            );

        string? protonPath = FindLinuxProtonPath();
        if (protonPath == null)
            throw new FileNotFoundException(
                "Could not find Proton installation. Ensure a Proton version is installed via Steam."
            );

        string? compatDataPath = null;
        foreach (var folder in GetLinuxSteamLibraryFolders())
        {
            if (gamePath.StartsWith(Path.Combine(folder, "steamapps", "common")))
            {
                compatDataPath = Path.Combine(folder, "steamapps", "compatdata", SteamGameId);
                break;
            }
        }

        if (compatDataPath == null)
            throw new DirectoryNotFoundException("Could not find Steam compatdata directory.");

        string? steamClientPath = GetLinuxSteamRoots().FirstOrDefault();
        if (steamClientPath == null)
            throw new DirectoryNotFoundException("Could not find Steam installation.");

        string args = string.Join(" ", GetRLBotArgs(gamePort));

        Process rocketLeague = new();
        rocketLeague.StartInfo.FileName = protonPath;
        rocketLeague.StartInfo.ArgumentList.Add("run");
        rocketLeague.StartInfo.ArgumentList.Add(gamePath);
        foreach (var arg in GetRLBotArgs(gamePort))
            rocketLeague.StartInfo.ArgumentList.Add(arg);
        rocketLeague.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        rocketLeague.StartInfo.Environment["STEAM_COMPAT_CLIENT_INSTALL_PATH"] =
            steamClientPath;
        rocketLeague.StartInfo.Environment["STEAM_COMPAT_DATA_PATH"] = compatDataPath;
        rocketLeague.StartInfo.Environment["STEAM_COMPAT_APP_ID"] = SteamGameId;
        rocketLeague.StartInfo.Environment["SteamAppId"] = SteamGameId;
        rocketLeague.StartInfo.Environment["SteamGameId"] = SteamGameId;

        Logger.LogInformation(
            $"Starting Rocket League via Proton without EAC: {protonPath} run {gamePath} {args}"
        );
        rocketLeague.Start();
    }
}
#endif
