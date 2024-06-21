using rlbot.flat;

namespace RLBotCS.Server.BridgeMessage;

internal record SetMutators(MutatorSettingsT MutatorSettings) : IBridgeMessage
{
    public void HandleMessage(BridgeContext context)
    {
        context.GameState.GameTimeRemaining = MutatorSettings.MatchLength switch
        {
            MatchLengthMutator.FiveMinutes => 5 * 60,
            MatchLengthMutator.TenMinutes => 10 * 60,
            MatchLengthMutator.TwentyMinutes => 20 * 60,
            MatchLengthMutator.Unlimited => 0,
            _ => throw new ArgumentOutOfRangeException(
                nameof(MutatorSettings.MatchLength),
                MutatorSettings.MatchLength,
                null
            ),
        };

        context.GameState.MatchLength = MutatorSettings.MatchLength switch
        {
            MatchLengthMutator.FiveMinutes => Bridge.Packet.MatchLength.FiveMinutes,
            MatchLengthMutator.TenMinutes => Bridge.Packet.MatchLength.TenMinutes,
            MatchLengthMutator.TwentyMinutes => Bridge.Packet.MatchLength.TwentyMinutes,
            MatchLengthMutator.Unlimited => Bridge.Packet.MatchLength.Unlimited,
            _ => throw new ArgumentOutOfRangeException(
                nameof(MutatorSettings.MatchLength),
                MutatorSettings.MatchLength,
                null
            ),
        };

        context.GameState.RespawnTime = MutatorSettings.RespawnTime switch
        {
            RespawnTimeMutator.ThreeSeconds => 3,
            RespawnTimeMutator.TwoSeconds => 2,
            RespawnTimeMutator.OneSecond => 1,
            RespawnTimeMutator.DisableGoalReset => 3,
            _ => throw new ArgumentOutOfRangeException(
                nameof(MutatorSettings.RespawnTime),
                MutatorSettings.RespawnTime,
                null
            ),
        };
    }
}