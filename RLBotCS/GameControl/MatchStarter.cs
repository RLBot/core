using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.GameState;
using RLBotCS.Server;
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
        private TypedPayload? lastMatchSettings;
        private GravityOption gravityOption = GravityOption.Default;

        public MatchStarter(TcpMessenger tcpMessenger, GameState.GameState gameState)
        {
            this.playerMapping = gameState.playerMapping;
            this.matchCommandSender = new MatchCommandSender(tcpMessenger);
        }

        public void HandleMatchSettings(rlbot.flat.MatchSettingsT matchSettings, TypedPayload originalMessage)
        {
            if (matchSettings.MutatorSettings is MutatorSettingsT mutatorSettings) {
                gravityOption = mutatorSettings.GravityOption;
            }

            // Load the map, then spawn the players AFTER the map loads.
            var load_map_command = FlatToCommand.MakeOpenCommand(matchSettings);
            Console.WriteLine("Start match with command: " + load_map_command);
            matchCommandSender.AddCommand(load_map_command);
            matchCommandSender.Send();

            Console.WriteLine("RLBotCS will wait 3 seconds for the map to load...");
            Thread.Sleep(3000);
            Console.WriteLine("RLBotCS is done waiting, spawning bots...");

            for (int i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
            {
                var playerConfig = matchSettings.PlayerConfigurations[i];

                var alreadySpawnedPlayer = playerMapping.getKnownPlayers().FirstOrDefault((kp) => playerConfig.SpawnId == kp.spawnId);
                if (alreadySpawnedPlayer != null)
                {
                    // We've already spawned this player, don't duplicate them.
                    continue;
                }

                var loadout = FlatToModel.ToLoadout(playerConfig.Loadout, playerConfig.Team);

                Console.WriteLine("Spawning player " + playerConfig.Name + " with spawn id " + playerConfig.SpawnId);

                switch (playerConfig.Variety.Type)
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
                        var skill = playerConfig.Variety.AsPsyonixBotPlayer().BotSkill;
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
                        Console.WriteLine("Unable to spawn player with variety type: " + playerConfig.Variety.Type);
                        break;
                }
            }

            matchCommandSender.Send();

            lastMatchSettings = originalMessage;
        }

        public TypedPayload? GetMatchSettings() {
            return lastMatchSettings;
        }

        public float GetGravity()
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
