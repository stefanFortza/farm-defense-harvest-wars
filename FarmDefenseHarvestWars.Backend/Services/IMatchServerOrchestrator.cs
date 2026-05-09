using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IMatchServerOrchestrator
{
    Task<MatchServerEndpoint> StartMatchServerAsync(
        string matchId,
        IReadOnlyCollection<UnitUnlockDto> defenderDeck,
        IReadOnlyCollection<UnitUnlockDto> attackerDeck,
        int defenderAvatarIndex,
        int attackerAvatarIndex,
        string defenderName,
        string attackerName,
        CancellationToken cancellationToken = default);
}

public sealed record MatchServerEndpoint(string Host, int Port);
