using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IMatchmakingService
{
    Task<MatchmakingStatusDto> QueueForMatchAsync(string userId, CancellationToken cancellationToken = default);
    void CancelMatchmaking(string userId);
    MatchmakingStatusDto GetStatusForUser(string userId);
    void CompleteMatch(string matchId);
}
