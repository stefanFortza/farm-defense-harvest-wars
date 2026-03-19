using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IMatchServerOrchestrator
{
    Task<MatchServerEndpoint> StartMatchServerAsync(
        string matchId,
        IReadOnlyCollection<UnitType> defenderDeck,
        IReadOnlyCollection<UnitType> attackerDeck,
        CancellationToken cancellationToken = default);
}

public sealed record MatchServerEndpoint(string Host, int Port);
