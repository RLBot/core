using Microsoft.Extensions.Logging;

namespace RLBotCS.ManagerTools;

public class CustomMap
{
    private static readonly ILogger Logger = Logging.GetLogger("CustomMap");

    public const string RL_MAP_KEY = "Haunted_TrainStation_P";
    private const string MAP_SACRIFICE = RL_MAP_KEY + ".upk";
    private const string TEMP_MAP_NAME = RL_MAP_KEY + "_copy.upk";

    public static bool IsCustomMap(string path)
    {
        bool isMapPath = path.EndsWith(".upk") || path.EndsWith(".udk");
        return isMapPath && File.Exists(path);
    }

    private static string GetMapsBasePath()
    {
        // rocketleague/Binaries/Win64/RocketLeague.exe
        string? gamePath = LaunchManager.GetRocketLeaguePath();
        if (gamePath == null)
        {
            // throw exception because we shouldn't construct this class before Rocket League has launched
            throw new InvalidOperationException("Could not find Rocket League executable");
        }

        // rocketleague/
        string basePath = Path.GetDirectoryName(
            Path.GetDirectoryName(Path.GetDirectoryName(gamePath)!)!
        )!;
        // rocketleague/TAGame/CookedPCConsole
        return Path.Combine(basePath, "TAGame", "CookedPCConsole");
    }

    private string _originalMapPath;
    private string _tempMapPath;

    public CustomMap(string path)
    {
        if (!IsCustomMap(path))
        {
            // throw exception because we shouldn't construct this class with an invalid path
            throw new ArgumentException("Provided path is not a valid custom map");
        }

        string mapsBasePath = GetMapsBasePath();
        _originalMapPath = Path.Combine(mapsBasePath, MAP_SACRIFICE);
        _tempMapPath = Path.Combine(mapsBasePath, TEMP_MAP_NAME);

        Logger.LogInformation($"Custom map detected, loading into {RL_MAP_KEY}");

        // don't overwrite the original map if it already exists
        if (!File.Exists(_tempMapPath))
        {
            // copy the original map to a temporary file so we can restore it later
            File.Copy(_originalMapPath, _tempMapPath, false);
        }

        // replace the original map with the custom map
        File.Copy(path, _originalMapPath, true);
    }

    public void TryRestoreOriginalMap()
    {
        if (!File.Exists(_tempMapPath))
            return;

        File.Copy(_tempMapPath, _originalMapPath, true);
        File.Delete(_tempMapPath);
        Logger.LogInformation($"Restored original map of {RL_MAP_KEY}");
    }

    ~CustomMap()
    {
        // Doing this in the destructor to ensure that the original map is restored,
        // even if an invalid state occurs elsewhere
        TryRestoreOriginalMap();
    }
}
