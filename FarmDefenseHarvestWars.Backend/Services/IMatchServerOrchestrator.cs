using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IMatchServerOrchestrator
{
    Task<MatchServerEndpoint> StartMatchServerAsync(
        string matchId,
        IReadOnlyCollection<UnitUnlockDto> defenderDeck,
        IReadOnlyCollection<UnitUnlockDto> attackerDeck,
        CancellationToken cancellationToken = default);
}

public sealed record MatchServerEndpoint(string Host, int Port);
