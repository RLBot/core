using Microsoft.Extensions.Logging;
using rlbot.flat;
using RLBotCS.Conversion;

namespace RLBotCS.ManagerTools;

public class ConfigValidator
{
    public static readonly ILogger Logger = Logging.GetLogger("ConfigValidator");

    /// <summary>
    /// Validates the given match config.
    /// The validation may modify fields (e.g. turning null or unused strings into empty strings).
    /// Psyonix bots will be given Psyonix preset loadouts as fitting.
    /// If the config is invalid, the reasons are logged.
    /// </summary>
    /// <returns>Whether the given match is valid can be started without issues.</returns>
    public static bool Validate(MatchConfigurationT config)
    {
        bool valid = true;
        PsyonixLoadouts.Reset();

        if (config.Launcher == Launcher.Custom)
        {
            config.LauncherArg = (config.LauncherArg ?? "").ToLower();
            if (config.LauncherArg != "legendary")
            {
                Logger.LogError($"Invalid custom launcher argument '{config.LauncherArg}'. Only 'legendary' is supported currently.");
                valid = false;

            }
        }
        else
        {
            config.LauncherArg = "";
        }

        valid = ValidatePlayers(config.PlayerConfigurations) && valid;
        valid = ValidateScripts(config.ScriptConfigurations) && valid;
        
        return valid;
    }

    private static bool ValidatePlayers(List<PlayerConfigurationT> players)
    {
        bool valid = true;
        int humanCount = 0;
        
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            
            if (player.Team != 0 && player.Team != 1)
            {
                Logger.LogError($"Car with index {i} has invalid team '{player.Team}'. " +
                                $"Must be 0 (blue) or 1 (orange).");
                valid = false;
            }
            
            switch (player.Variety.Type)
            {
                case PlayerClass.CustomBot:
                    player.AgentId ??= "";
                    if (player.AgentId == "")
                    {
                        Logger.LogError($"Car with index {i} has type 'rlbot' but an empty agent ID. " +
                                        $"RLBot bots must have an agent ID. " +
                                        $"We recommend the format \"<developer>/<botname>/<version>\"");
                        valid = false;
                    }
                    player.Name ??= "";
                    player.RunCommand ??= "";
                    player.RootDir ??= "";
                    break;
                case PlayerClass.Psyonix:
                    string skill = player.Variety.AsPsyonix().BotSkill switch
                    {
                        PsyonixSkill.Beginner => "beginner",
                        PsyonixSkill.Rookie => "rookie",
                        PsyonixSkill.Pro => "pro",
                        PsyonixSkill.AllStar => "allstar",
                    };
                    player.AgentId ??= "psyonix/" + skill;  // Not that it really matters
                    
                    // Apply Psyonix preset loadouts
                    if (player.Name == null)
                    {
                        (player.Name, var presetLoadout) = PsyonixLoadouts.GetNext((int)player.Team);
                        player.Loadout ??= presetLoadout;
                    }
                    else if (player.Loadout == null)
                    {
                        player.Loadout = PsyonixLoadouts.GetFromName(player.Name, (int)player.Team);
                    }
                    
                    player.RunCommand = "";
                    player.RootDir = "";
                    
                    break;
                case PlayerClass.Human:
                    humanCount++;
                    player.AgentId = "human";  // Not that it really matters
                    player.Name = "";
                    player.Loadout = null;
                    player.RunCommand = "";
                    player.RootDir = "";
                    break;
                case PlayerClass.PartyMember:
                    Logger.LogError("PartyMember bot type not supported yet.");
                    valid = false;
                    break;
            }
        }

        if (humanCount > 1)
        {
            Logger.LogError("Only one player can be of type 'Human'.");
            valid = false;
        }

        return valid;
    }

    private static bool ValidateScripts(List<ScriptConfigurationT> scripts)
    {
        bool valid = true;
        
        for (int i = 0; i < scripts.Count; i++)
        {
            var script = scripts[i];

            script.AgentId ??= "";
            if (script.AgentId == "")
            {
                Logger.LogError($"Script with index {i} has an empty agent ID. " +
                                $"Scripts must have an agent ID. " +
                                $"We recommend the format \"<developer>/<scriptname>/<version>\"");
                valid = false;
            }
            script.Name ??= "";
            script.RunCommand ??= "";
            script.RootDir ??= "";
        }

        return valid;
    }
}
