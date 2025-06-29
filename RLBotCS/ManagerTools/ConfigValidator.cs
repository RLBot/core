using Microsoft.Extensions.Logging;
using RLBot.Flat;
using RLBotCS.Conversion;
using RLBotCS.Model;

namespace RLBotCS.ManagerTools;

using Fields = ConfigParser.Fields;

public static class ConfigValidator
{
    private static readonly ILogger Logger = Logging.GetLogger("ConfigValidator");

    /// <summary>
    /// Validates the given match config.
    /// The validation may modify fields (e.g. turning null or unused strings into empty strings).
    /// Psyonix bots will be given Psyonix preset loadouts as fitting.
    /// If the config is invalid, the reasons are logged.
    /// </summary>
    /// <returns>Whether the given match is valid and can be started without issues.</returns>
    public static bool Validate(MatchConfigurationT config)
    {
        bool valid = true;
        PsyonixLoadouts.Reset();
        ConfigContextTracker ctx = new();

        using (ctx.Begin(Fields.RlBotTable))
        {
            if (config.Launcher == Launcher.Custom)
            {
                config.LauncherArg = (config.LauncherArg ?? "").ToLower();
                if (config.LauncherArg != "legendary" && config.LauncherArg != "heroic")
                {
                    Logger.LogError(
                        $"Invalid {ctx.ToStringWithEnd(Fields.RlBotLauncherArg)} value \"{config.LauncherArg}\". "
                            + $"\"legendary\" and \"heroic\" are the only Custom launchers supported currently."
                    );
                    valid = false;
                }
            }
            else
            {
                config.LauncherArg = "";
            }
        }

        config.Mutators ??= new();
        config.PlayerConfigurations ??= new();
        config.ScriptConfigurations ??= new();

        valid = ValidatePlayers(ctx, config.PlayerConfigurations) && valid;
        valid = ValidateScripts(ctx, config.ScriptConfigurations) && valid;

        Logger.LogDebug(valid ? "Match config is valid." : "Match config is invalid!");
        return valid;
    }

    private static bool ValidatePlayers(
        ConfigContextTracker ctx,
        List<PlayerConfigurationT> players
    )
    {
        bool valid = true;
        int humanCount = 0;
        int humanIndex = -1;

        for (int i = 0; i < players.Count; i++)
        {
            using var _ = ctx.Begin($"{Fields.CarsList}[{i}]");
            var player = players[i];

            if (player.Team != Team.Blue && player.Team != Team.Orange)
            {
                Logger.LogError(
                    $"Invalid {ctx.ToStringWithEnd(Fields.AgentTeam)} of '{player.Team}'. "
                        + $"Must be 0 (blue) or 1 (orange)."
                );
                valid = false;
            }

            switch (player.Variety.Value)
            {
                case CustomBotT bot:
                    bot.AgentId ??= "";
                    if (bot.AgentId == "")
                    {
                        Logger.LogError(
                            $"{ctx.ToStringWithEnd(Fields.AgentType)} is \"rlbot\" "
                                + $"but {ctx.ToStringWithEnd(Fields.AgentAgentId)} is empty. "
                                + $"RLBot bots must have an agent ID. "
                                + $"We recommend the format \"<developer>/<botname>/<version>\"."
                        );
                        valid = false;
                    }
                    bot.Name ??= "";
                    bot.RunCommand ??= "";
                    bot.RootDir ??= "";
                    bot.Loadout ??= new();
                    bot.Loadout.LoadoutPaint ??= new();

                    player.PlayerId = $"{bot.AgentId}/{player.Team}/{i}".GetHashCode();
                    break;
                case PsyonixBotT bot:
                    string skill = bot.BotSkill switch
                    {
                        PsyonixSkill.Beginner => "beginner",
                        PsyonixSkill.Rookie => "rookie",
                        PsyonixSkill.Pro => "pro",
                        PsyonixSkill.AllStar => "allstar",
                        _ => HandleOutOfRange(
                            out valid,
                            "",
                            $"{ctx.ToStringWithEnd(Fields.AgentSkill)} is out of range."
                        ),
                    };

                    // Apply Psyonix preset loadouts
                    if (string.IsNullOrEmpty(bot.Name))
                    {
                        (bot.Name, var preset) = PsyonixLoadouts.GetNext((int)player.Team);
                        string andPreset = bot.Loadout == null ? " and preset" : "";
                        bot.Loadout ??= preset;
                        Logger.LogDebug(
                            $"Gave unnamed Psyonix bot {ctx} a name{andPreset} ({bot.Name})"
                        );
                    }
                    else if (bot.Loadout == null)
                    {
                        bot.Loadout = PsyonixLoadouts.GetFromName(bot.Name, (int)player.Team);
                        Logger.LogDebug(
                            bot.Loadout == null
                                ? $"Failed to find a preset loadout for Psyonix bot {ctx} named \"{bot.Name}\"."
                                : $"Found preset loadout for Psyonix bot {ctx} named \"{bot.Name}\"."
                        );
                    }

                    // Fallback if above fails or user didn't include paints
                    bot.Loadout ??= new();
                    bot.Loadout.LoadoutPaint ??= new();

                    player.PlayerId =
                        $"psyonix/{bot.BotSkill}/{player.Team}/{i}".GetHashCode();
                    break;
                case HumanT:
                    humanCount++;
                    humanIndex = i;
                    player.PlayerId = 0;
                    break;
            }
        }

        if (humanCount > 1)
        {
            Logger.LogError("Only one player can be of type \"Human\".");
            valid = false;
        }

        if (humanIndex != -1)
        {
            // Move human to last index
            var tmp = players[humanIndex];
            players[humanIndex] = players.Last();
            players[players.Count - 1] = tmp;
        }

        return valid;
    }

    private static bool ValidateScripts(
        ConfigContextTracker ctx,
        List<ScriptConfigurationT> scripts
    )
    {
        bool valid = true;

        for (int i = 0; i < scripts.Count; i++)
        {
            using var _ = ctx.Begin($"{Fields.ScriptsList}[{i}]");
            var script = scripts[i];

            script.AgentId ??= "";
            if (script.AgentId == "")
            {
                Logger.LogError(
                    $"{ctx.ToStringWithEnd(Fields.AgentAgentId)} is empty. "
                        + $"Scripts must have an agent ID. "
                        + $"We recommend the format \"<developer>/<scriptname>/<version>\"."
                );
                valid = false;
            }
            script.Name ??= "";
            script.RunCommand ??= "";
            script.RootDir ??= "";
            script.ScriptId = $"{script.AgentId}/{Team.Scripts}/{i}".GetHashCode();
        }

        return valid;
    }

    /// <summary>Helder function to handle enums out of range inline</summary>
    private static T HandleOutOfRange<T>(out bool valid, T fallback, string message)
    {
        valid = false;
        Logger.LogError(message);
        return fallback;
    }
}
