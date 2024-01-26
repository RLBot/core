using MatchManagement;
using rlbot.flat;
using RLBotCS.Conversion;
using RLBotCS.GameState;
using RLBotCS.Server;
using RLBotModels.Message;
using RLBotSecret.Controller;
using RLBotSecret.Conversion;
using RLBotSecret.TCP;
using GameStateType = rlbot.flat.GameStateType;

namespace RLBotCS.GameControl
{
    internal class MatchStarter
    {
        private PlayerMapping playerMapping;
        private MatchCommandSender matchCommandSender;
        private (MatchSettingsT, TypedPayload)? lastMatchMessage;
        private MatchLength matchLength = rlbot.flat.MatchLength.Five_Minutes;
        private float respawnTime = 3;
        private bool needsSpawnBots = true;
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

        public void HandleMatchSettings(
            MatchSettingsT matchSettings,
            TypedPayload originalMessage,
            GameStateType gameStateType
        )
        {
            if (matchSettings.MutatorSettings is MutatorSettingsT mutatorSettings)
            {
                matchLength = mutatorSettings.MatchLength;
                matchCommandSender.AddConsoleCommand(
                    FlatToCommand.MakeGravityCommandFromOption(mutatorSettings.GravityOption)
                );
                matchCommandSender.AddConsoleCommand(
                    FlatToCommand.MakeGameSpeedCommandFromOption(mutatorSettings.GameSpeedOption)
                );

                if (mutatorSettings.RespawnTimeOption is RespawnTimeOption respawnTimeOption)
                {
                    if (respawnTimeOption == RespawnTimeOption.Two_Seconds)
                    {
                        respawnTime = 2;
                    }
                    else if (respawnTimeOption == RespawnTimeOption.One_Seconds)
                    {
                        respawnTime = 1;
                    }
                    else
                    {
                        respawnTime = 3;
                    }
                }
            }

            isStateSettingEnabled = matchSettings.EnableStateSetting;
            isRenderingEnabled = matchSettings.EnableRendering;

            if (matchSettings.AutoSaveReplay)
            {
                matchCommandSender.AddConsoleCommand(FlatToCommand.MakeAutoSaveReplayCommand());
            }

            var shouldSpawnNewMap = true;

            if (matchSettings.ExistingMatchBehavior == ExistingMatchBehavior.Continue_And_Spawn)
            {
                shouldSpawnNewMap = !hasEverLoadedMap || gameStateType == GameStateType.Ended;
            }
            else if (matchSettings.ExistingMatchBehavior == ExistingMatchBehavior.Restart_If_Different)
            {
                shouldSpawnNewMap = isDifferentFromLast(matchSettings);
            }

            lastMatchMessage = (matchSettings, originalMessage);

            if (shouldSpawnNewMap)
            {
                // Load the map, then spawn the players AFTER the map loads.
                var load_map_command = FlatToCommand.MakeOpenCommand(matchSettings);
                Console.WriteLine("Core is about to start match with command: " + load_map_command);
                matchCommandSender.AddConsoleCommand(load_map_command);
                matchCommandSender.Send();
                needsSpawnBots = true;
                hasEverLoadedMap = true;
            }
            else
            {
                // No need to load a new map, just spawn the players.
                SpawnBots(matchSettings);
            }
        }

        public bool isDifferentFromLast(MatchSettingsT matchSettings)
        {
            // don't consider rendering/state setting because that can be enable/disabled without restarting the match

            var lastMatchSettings = lastMatchMessage?.Item1;
            if (lastMatchSettings == null)
            {
                return true;
            }

            if (lastMatchSettings.PlayerConfigurations.Count != matchSettings.PlayerConfigurations.Count)
            {
                return true;
            }

            for (var i = 0; i < matchSettings.PlayerConfigurations.Count; i++)
            {
                var lastPlayerConfig = lastMatchSettings.PlayerConfigurations[i];
                var playerConfig = matchSettings.PlayerConfigurations[i];

                if (lastPlayerConfig.SpawnId != playerConfig.SpawnId)
                {
                    return true;
                }

                if (lastPlayerConfig.Team != playerConfig.Team)
                {
                    return true;
                }
            }

            if (lastMatchSettings.GameMode != matchSettings.GameMode)
            {
                return true;
            }

            if (lastMatchSettings.GameMapUpk != matchSettings.GameMapUpk)
            {
                return true;
            }

            if (lastMatchSettings.InstantStart != matchSettings.InstantStart)
            {
                return true;
            }

            var lastMutators = lastMatchSettings.MutatorSettings;
            var mutators = matchSettings.MutatorSettings;

            if (lastMutators.MatchLength != mutators.MatchLength)
            {
                return true;
            }

            if (lastMutators.MaxScore != mutators.MaxScore)
            {
                return true;
            }

            if (lastMutators.OvertimeOption != mutators.OvertimeOption)
            {
                return true;
            }

            if (lastMutators.SeriesLengthOption != mutators.SeriesLengthOption)
            {
                return true;
            }

            if (lastMutators.GameSpeedOption != mutators.GameSpeedOption)
            {
                return true;
            }

            if (lastMutators.BallMaxSpeedOption != mutators.BallMaxSpeedOption)
            {
                return true;
            }

            if (lastMutators.BallTypeOption != mutators.BallTypeOption)
            {
                return true;
            }

            if (lastMutators.BallWeightOption != mutators.BallWeightOption)
            {
                return true;
            }

            if (lastMutators.BallSizeOption != mutators.BallSizeOption)
            {
                return true;
            }

            if (lastMutators.BallBouncinessOption != mutators.BallBouncinessOption)
            {
                return true;
            }

            if (lastMutators.BoostOption != mutators.BoostOption)
            {
                return true;
            }

            if (lastMutators.RumbleOption != mutators.RumbleOption)
            {
                return true;
            }

            if (lastMutators.BoostStrengthOption != mutators.BoostStrengthOption)
            {
                return true;
            }

            if (lastMutators.GravityOption != mutators.GravityOption)
            {
                return true;
            }

            if (lastMutators.DemolishOption != mutators.DemolishOption)
            {
                return true;
            }

            if (lastMutators.RespawnTimeOption != mutators.RespawnTimeOption)
            {
                return true;
            }

            return false;
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
            PlayerConfigurationT? humanConfig = null;
            var humanIndex = -1;
            var indexOffset = 0;

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

                switch (playerConfig.Variety.Type)
                {
                    case PlayerClass.RLBotPlayer:
                        Console.WriteLine(
                            "Core is spawning player "
                                + playerConfig.Name
                                + " with spawn id "
                                + playerConfig.SpawnId
                        );

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
                                desiredPlayerIndex = i - indexOffset,
                                isCustomBot = true,
                            }
                        );
                        break;
                    case PlayerClass.PsyonixBotPlayer:
                        var skill = playerConfig.Variety.AsPsyonixBotPlayer().BotSkill;
                        var skillEnum = BotSkill.Hard;
                        if (skill < 0.5)
                        {
                            skillEnum = BotSkill.Easy;
                        }
                        else if (skill < 1)
                        {
                            skillEnum = BotSkill.Medium;
                        }

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
                                desiredPlayerIndex = i - indexOffset,
                                isCustomBot = false
                            }
                        );
                        break;
                    case PlayerClass.HumanPlayer:
                        if (humanConfig != null)
                        {
                            // We can't spawn this human player,
                            // so we need to -1 for ever index after this
                            // to properly set the desired player indicies
                            indexOffset++;
                            Console.WriteLine(
                                "Warning: Multiple human players requested. RLBot only supports spawning max one human per match."
                            );
                        }
                        else
                        {
                            humanConfig = playerConfig;
                            // indexOffset can only ever be 0 here
                            humanIndex = i;
                        }

                        break;
                }
            }

            if (humanConfig != null)
            {
                matchCommandSender.Send();

                // For some reason if we send this command to early,
                // the game will only half-spawn us
                matchCommandSender.AddConsoleCommand("ChangeTeam " + humanConfig.Team);

                playerMapping.addPendingSpawn(
                    new SpawnTracker()
                    {
                        commandId = 0,
                        spawnId = humanConfig.SpawnId,
                        desiredPlayerIndex = humanIndex,
                        isCustomBot = false,
                    }
                );
            }
            else
            {
                // If no human was requested for the match,
                // then make the human spectate so we can start the match
                matchCommandSender.AddConsoleCommand("spectate");
            }

            matchCommandSender.Send();
        }

        public TypedPayload? GetMatchSettings()
        {
            return lastMatchMessage?.Item2;
        }

        public MatchLength MatchLength()
        {
            return matchLength;
        }

        public float RespawnTime()
        {
            return respawnTime;
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
