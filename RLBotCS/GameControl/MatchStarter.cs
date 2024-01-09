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
        private (MatchSettingsT, TypedPayload)? lastMatchMessage;
        private bool isUnlimitedTime = false;
        private bool needsSpawnBots = true;

        // TODO - does not return to false if map is unloaded
        private bool hasEverLoadedMap = false;
        private bool isStateSettingEnabled = true;
        private bool isRenderingEnabled = true;

        public MatchStarter(TcpMessenger tcpMessenger, GameState.GameState gameState)
        {
            this.playerMapping = gameState.playerMapping;
            this.matchCommandSender = new MatchCommandSender(tcpMessenger);
        }

        public void SetDesiredGameState(DesiredGameStateT desiredGameState, ushort? ballActorid)
        {
            if (desiredGameState.GameInfoState is DesiredGameInfoStateT gameState)
            {
                if (gameState.WorldGravityZ is FloatT worldGravityZ)
                {
                    matchCommandSender.AddConsoleCommand(FlatToCommand.MakeGravityCommand(worldGravityZ.Val));
                }

                if (gameState.GameSpeed is FloatT gameSpeed)
                {
                    matchCommandSender.AddConsoleCommand(FlatToCommand.MakeGameSpeedCommand(gameSpeed.Val));
                }

                if (gameState.Paused is BoolT paused)
                {
                    matchCommandSender.AddSetPausedCommand(paused.Val);
                }

                if (gameState.EndMatch is BoolT endMatch && endMatch.Val)
                {
                    matchCommandSender.AddMatchEndCommand();
                }
            }

            for (var i = 0; i < desiredGameState.CarStates.Count; i++)
            {
                if (
                    desiredGameState.CarStates[i] is DesiredCarStateT carState
                    && playerMapping.ActorIdFromPlayerIndex(i) is ushort actorId
                )
                {
                    if (carState.Physics is DesiredPhysicsT physics)
                    {
                        matchCommandSender.AddSetPhysicsCommand(actorId, FlatToModel.DesiredToPhysics(physics));
                    }

                    if (carState.BoostAmount is FloatT boostAmount)
                    {
                        matchCommandSender.AddSetBoostCommand(actorId, (int)boostAmount.Val);
                    }
                }
            }

            if (ballActorid is ushort)
            {
                if (desiredGameState.BallState is DesiredBallStateT ballState)
                {
                    if (ballState.Physics is DesiredPhysicsT physics)
                    {
                        matchCommandSender.AddSetPhysicsCommand(
                            ballActorid.Value,
                            FlatToModel.DesiredToPhysics(physics)
                        );
                    }
                }
            }

            matchCommandSender.Send();
        }

        public void HandleMatchSettings(MatchSettingsT matchSettings, TypedPayload originalMessage)
        {
            if (matchSettings.MutatorSettings is MutatorSettingsT mutatorSettings)
            {
                isUnlimitedTime = mutatorSettings.MatchLength == MatchLength.Unlimited;
                matchCommandSender.AddConsoleCommand(
                    FlatToCommand.MakeGravityCommandFromOption(mutatorSettings.GravityOption)
                );
                matchCommandSender.AddConsoleCommand(
                    FlatToCommand.MakeGameSpeedCommandFromOption(mutatorSettings.GameSpeedOption)
                );
            }

            isStateSettingEnabled = matchSettings.EnableStateSetting;
            isRenderingEnabled = matchSettings.EnableRendering;

            if (matchSettings.AutoSaveReplay)
            {
                matchCommandSender.AddConsoleCommand(FlatToCommand.MakeAutoSaveReplayCommand());
            }

            lastMatchMessage = (matchSettings, originalMessage);

            if (
                hasEverLoadedMap
                && matchSettings.ExistingMatchBehavior == ExistingMatchBehavior.Continue_And_Spawn
            )
            {
                // No need to load a new map, just spawn the players.
                SpawnBots(matchSettings);
            }
            else
            {
                // Load the map, then spawn the players AFTER the map loads.
                var load_map_command = FlatToCommand.MakeOpenCommand(matchSettings);
                Console.WriteLine("Core is about to start match with command: " + load_map_command);
                matchCommandSender.AddConsoleCommand(load_map_command);
                matchCommandSender.Send();
                needsSpawnBots = true;
                hasEverLoadedMap = true;
            }
        }

        public void applyMessageBundle(MessageBundle messageBundle)
        {
            if (needsSpawnBots && lastMatchMessage?.Item1 is MatchSettingsT matchSettings)
            {
                if (messageBundle.messages.Any(message => message is MatchInfo))
                {
                    SpawnBots(matchSettings);
                    needsSpawnBots = false;
                }
            }
        }

        private void SpawnBots(MatchSettingsT matchSettings)
        {
            for (int i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
            {
                var playerConfig = matchSettings.PlayerConfigurations[i];

                var alreadySpawnedPlayer = playerMapping
                    .getKnownPlayers()
                    .FirstOrDefault((kp) => playerConfig.SpawnId == kp.spawnId);
                if (alreadySpawnedPlayer != null)
                {
                    // We've already spawned this player, don't duplicate them.
                    continue;
                }

                var loadout = FlatToModel.ToLoadout(playerConfig.Loadout, playerConfig.Team);

                Console.WriteLine(
                    "Core is spawning player " + playerConfig.Name + " with spawn id " + playerConfig.SpawnId
                );

                switch (playerConfig.Variety.Type)
                {
                    case PlayerClass.RLBotPlayer:
                        var rlbotSpawnCommandId = matchCommandSender.AddBotSpawnCommand(
                            playerConfig.Name,
                            playerConfig.Team,
                            BotSkill.Custom,
                            loadout
                        );

                        playerMapping.addPendingSpawn(
                            new SpawnTracker()
                            {
                                commandId = rlbotSpawnCommandId,
                                spawnId = playerConfig.SpawnId,
                                desiredPlayerIndex = i,
                                isCustomBot = true,
                            }
                        );
                        break;
                    case PlayerClass.PsyonixBotPlayer:
                        var skill = playerConfig.Variety.AsPsyonixBotPlayer().BotSkill;
                        var skillEnum =
                            skill < 0.5
                                ? BotSkill.Easy
                                : skill < 1
                                    ? BotSkill.Medium
                                    : BotSkill.Hard;
                        var psySpawnCommandId = matchCommandSender.AddBotSpawnCommand(
                            playerConfig.Name,
                            playerConfig.Team,
                            skillEnum,
                            loadout
                        );

                        playerMapping.addPendingSpawn(
                            new SpawnTracker()
                            {
                                commandId = psySpawnCommandId,
                                spawnId = playerConfig.SpawnId,
                                desiredPlayerIndex = i,
                                isCustomBot = false
                            }
                        );
                        break;
                }
            }

            matchCommandSender.AddConsoleCommand("spectate");

            matchCommandSender.Send();
        }

        public TypedPayload? GetMatchSettings()
        {
            return lastMatchMessage?.Item2;
        }

        public bool IsUnlimitedTime()
        {
            return isUnlimitedTime;
        }

        internal bool IsStateSettingEnabled()
        {
            return isStateSettingEnabled;
        }

        internal bool IsRenderingEnabled()
        {
            return isRenderingEnabled;
        }
    }
}
