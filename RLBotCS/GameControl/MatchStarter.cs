using rlbot.flat;
using RLBotCS.GameState;
using RLBotModels.Command;
using RLBotModels.Message;
using RLBotSecret.Controller;
using RLBotSecret.Conversion;
using RLBotSecret.TCP;

namespace RLBotCS.GameControl
{
    internal class MatchStarter
    {
        private PlayerMapping playerMapping;
        private MatchCommandSender matchCommandSender;

        public MatchStarter(TcpMessenger tcpMessenger, GameState.GameState gameState)
        {
            this.playerMapping = gameState.playerMapping;
            this.matchCommandSender = new MatchCommandSender(tcpMessenger);
        }

        public void HandleMatchSettings(rlbot.flat.MatchSettings matchSettings)
        {
            // TODO: load the map, then spawn the players AFTER the map loads.

            for (int i = 0; i < matchSettings.PlayerConfigurationsLength; i++)
            {
                var playerConfig = matchSettings.PlayerConfigurations(i).Value;

                var alreadySpawnedPlayer = playerMapping.getKnownPlayers().FirstOrDefault((kp) => playerConfig.SpawnId == kp.spawnId);
                if (alreadySpawnedPlayer != null)
                {
                    // We've already spawned this player, don't duplicate them.
                    continue;
                }

                var loadout = FlatToModel.ToLoadout(playerConfig.Loadout.Value, playerConfig.Team);

                switch (playerConfig.VarietyType)
                {
                    case PlayerClass.RLBotPlayer:
                        var rlbotSpawnCommandId = matchCommandSender.AddBotSpawn(playerConfig.Name, playerConfig.Team, BotSkill.Custom, loadout);

                        playerMapping.addPendingSpawn(new SpawnTracker()
                        {
                            commandId = rlbotSpawnCommandId,
                            spawnId = playerConfig.SpawnId,
                            desiredPlayerIndex = i,
                            isCustomBot = true,
                        });
                        break;
                    case PlayerClass.PsyonixBotPlayer:
                        var skill = playerConfig.VarietyAsPsyonixBotPlayer().BotSkill;
                        var skillEnum = skill < 0.5 ? BotSkill.Easy : skill < 1 ? BotSkill.Medium : BotSkill.Hard;
                        var psySpawnCommandId = matchCommandSender.AddBotSpawn(playerConfig.Name, playerConfig.Team, skillEnum, loadout);

                        playerMapping.addPendingSpawn(new SpawnTracker()
                        {
                            commandId = psySpawnCommandId,
                            spawnId = playerConfig.SpawnId,
                            desiredPlayerIndex = i,
                            isCustomBot = false
                        });
                        break;
                    default:
                        Console.WriteLine("Unable to spawn player with variety type: " + playerConfig.VarietyType);
                        break;
                }
                
            }

            matchCommandSender.Send();
        }

        static float getGravity(GravityOption gravityOption)
        {
            return gravityOption switch
            {
                GravityOption.Low => -325,
                GravityOption.High => -1137.5f,
                GravityOption.Super_High => -3250,
                _ => -650,
            };
        }
    }
}
