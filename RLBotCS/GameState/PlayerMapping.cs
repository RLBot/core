using RLBotModels.Message;

namespace RLBotCS.GameState
{
    internal class PlayerMapping
    {
        private List<SpawnTracker> pendingSpawns = new();

        private Dictionary<ushort, uint> actorIdToPlayerIndex = new();
        private Dictionary<uint, PlayerMetadata> playerIndexToMetadata = new();

        private void registerActorId(ushort actorId, uint playerIndex)
        {
            actorIdToPlayerIndex[actorId] = playerIndex;
            if (playerIndexToMetadata.ContainsKey(playerIndex))
            {
                playerIndexToMetadata[playerIndex].actorId = actorId;
            }
        }

        public PlayerMetadata applyCarSpawn(CarSpawn carSpawn)
        {
            var playerMetadata = getPlayerMetadata(carSpawn);
            playerIndexToMetadata[(uint)playerMetadata.playerIndex] = playerMetadata;
            actorIdToPlayerIndex[playerMetadata.actorId] = (uint)playerMetadata.playerIndex;
            return playerMetadata;
        }

        public void addPendingSpawn(SpawnTracker spawnTracker)
        {
            pendingSpawns.Add(spawnTracker);
        }

        private PlayerMetadata getPlayerMetadata(CarSpawn carSpawn)
        {
            var spawnTracker = spawnTrackerFromCommandId(carSpawn.commandId);

            if (spawnTracker != null)
            {
                var playerIndex = spawnTracker.desiredPlayerIndex;
                if (playerIndexToMetadata.ContainsKey(spawnTracker.desiredPlayerIndex))
                {
                    // There's a conflict!
                    playerIndex = findUnusedPlayerIndex();
                }

                return new PlayerMetadata()
                {
                    playerIndex = playerIndex,
                    isCustomBot = spawnTracker.isCustomBot,
                    actorId = carSpawn.actorId,
                    spawnId = spawnTracker.spawnId,
                };
            }
            else
            {
                return new PlayerMetadata()
                {
                    playerIndex = findUnusedPlayerIndex(),
                    isCustomBot = false,
                    actorId = carSpawn.actorId
                };
            }
        }

        internal IEnumerable<ushort> getCustomBotActorIds()
        {
            return playerIndexToMetadata.Values.Where(pmd => pmd.isCustomBot).Select(pmd => pmd.actorId);
        }

        internal IEnumerable<PlayerMetadata> getKnownPlayers()
        {
            return playerIndexToMetadata.Values;
        }

        internal uint? PlayerIndexFromActorId(ushort actorId)
        {
            uint playerIndex;
            if (actorIdToPlayerIndex.TryGetValue(actorId, out playerIndex))
            {
                return playerIndex;
            }
            return null;
        }

        internal ushort? ActorIdFromPlayerIndex(uint playerIndex)
        {
            if (playerIndexToMetadata.TryGetValue(playerIndex, out var playerMetadata))
            {
                return playerMetadata.actorId;
            }

            return null;
        }

        internal PlayerMetadata? tryRemoveActorId(ushort actorId)
        {
            if (actorIdToPlayerIndex.ContainsKey(actorId))
            {
                var playerIndex = actorIdToPlayerIndex[actorId];
                var playerMetadata = playerIndexToMetadata[playerIndex];
                if (playerMetadata != null)
                {
                    playerIndexToMetadata.Remove(playerIndex);
                    actorIdToPlayerIndex.Remove(actorId);
                    return playerMetadata;
                }
            }
            return null;
        }

        private uint findUnusedPlayerIndex()
        {
            for (uint candidate = 0; ; candidate++)
            {
                if (
                    !playerIndexToMetadata.ContainsKey(candidate)
                    && !pendingSpawns.Any(s => s.desiredPlayerIndex == candidate)
                )
                {
                    return candidate;
                }
            }
        }

        private SpawnTracker? spawnTrackerFromCommandId(ushort command_id)
        {
            foreach (SpawnTracker spawnTracker in pendingSpawns)
            {
                if (spawnTracker.commandId == command_id)
                {
                    return spawnTracker;
                }
            }
            return null;
        }
    }
}
