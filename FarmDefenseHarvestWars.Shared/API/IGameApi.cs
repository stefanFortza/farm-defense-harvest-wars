using Refit;
using FarmDefenseHarvestWars.Shared.Models.Auth;
using FarmDefenseHarvestWars.Shared.Models.Game;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Constants;
using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Shared.API;

public interface IGameApi
{
    [Post(ApiRoutes.Register)]
    Task RegisterAsync([Body] RegisterRequestDto request);

    [Post(ApiRoutes.Login)]
    Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto request);

    [Get(ApiRoutes.Profile)]
    [Headers("Authorization: Bearer")]
    Task<PlayerProfileDto> GetProfileAsync();

    [Get(ApiRoutes.DeckByRole)]
    [Headers("Authorization: Bearer")]
    Task<DeckDto> GetDeckAsync([AliasAs("role")] PlayerRole role);

    [Get(ApiRoutes.DefaultDeckByRole)]
    [Headers("Authorization: Bearer")]
    Task<DeckDto> GetDefaultDeckAsync([AliasAs("role")] PlayerRole role);

    [Put(ApiRoutes.DeckByRole)]
    [Headers("Authorization: Bearer")]
    Task<DeckDto> UpdateDeckAsync([AliasAs("role")] PlayerRole role, [Body] UpdateDeckDto request);

    [Post(ApiRoutes.UnlockUnit)]
    [Headers("Authorization: Bearer")]
    Task<PlayerProfileDto> UnlockUnitAsync([AliasAs("unitType")] UnitType unitType);

    [Post(ApiRoutes.MatchmakingQueue)]
    [Headers("Authorization: Bearer")]
    Task<MatchmakingStatusDto> QueueForMatchAsync();

    [Delete(ApiRoutes.MatchmakingQueue)]
    [Headers("Authorization: Bearer")]
    Task CancelMatchmakingAsync();

    [Get(ApiRoutes.MatchmakingStatus)]
    [Headers("Authorization: Bearer")]
    Task<MatchmakingStatusDto> GetMatchmakingStatusAsync();

    [Post(ApiRoutes.MatchComplete)]
    Task CompleteMatchAsync(
        [AliasAs("matchId")] string matchId,
        [Body] MatchCompletionRequestDto request,
        [Header("X-Match-Server-Key")] string? callbackKey = null);
}
