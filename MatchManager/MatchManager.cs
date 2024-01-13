using rlbot.flat;

namespace MatchConfigManager
{
    public class MatchManager
    {
        public ConfigParser configParser = new();
        public MatchSettingsT matchSettings = new();
        public string defaultConfigPath = "rlbot.toml";

        public void LoadConfig(string path)
        {
            if (path.Length == 0)
            {
                path = defaultConfigPath;
            }
            Console.WriteLine("Reading config file at " + path);
            configParser.GetMatchSettings(path, matchSettings);
        }
    }

}
