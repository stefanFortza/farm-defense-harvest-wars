using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IMatchmakingService
{
    Task<MatchmakingStatusDto> QueueForMatchAsync(string userId, PlayerRole preferredRole = PlayerRole.Any, CancellationToken cancellationToken = default);
    void CancelMatchmaking(string userId);
    MatchmakingStatusDto GetStatusForUser(string userId);
    Task CompleteMatchAsync(string matchId, string callbackKey, MatchCompletionRequestDto request);
    Task<MatchRewardDto?> GetMatchRewardAsync(string matchId, string userId);
}
