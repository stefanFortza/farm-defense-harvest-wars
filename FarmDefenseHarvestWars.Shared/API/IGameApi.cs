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

    [Post(ApiRoutes.UpdateAvatar)]
    [Headers("Authorization: Bearer")]
    Task<PlayerProfileDto> UpdateAvatarAsync([AliasAs("avatarIndex")] int avatarIndex);

    [Post(ApiRoutes.MatchmakingQueue)]
    [Headers("Authorization: Bearer")]
    Task<MatchmakingStatusDto> QueueForMatchAsync([Query] PlayerRole preferredRole = PlayerRole.Any);

    [Delete(ApiRoutes.MatchmakingQueue)]
    [Headers("Authorization: Bearer")]
    Task CancelMatchmakingAsync();

    [Get(ApiRoutes.MatchmakingStatus)]
    [Headers("Authorization: Bearer")]
    Task<MatchmakingStatusDto> GetMatchmakingStatusAsync();

    [Get(ApiRoutes.MatchReward)]
    [Headers("Authorization: Bearer")]
    Task<MatchRewardDto> GetMatchRewardAsync([AliasAs("matchId")] string matchId);

    [Post(ApiRoutes.OpenChest)]
    [Headers("Authorization: Bearer")]
    Task<ChestOpenResultDto> OpenChestAsync([AliasAs("chestId")] string chestId);

    [Post(ApiRoutes.StartUnlockChest)]
    [Headers("Authorization: Bearer")]
    Task<PlayerProfileDto> StartUnlockChestAsync([AliasAs("chestId")] string chestId);

    [Post(ApiRoutes.UpgradeUnit)]
    [Headers("Authorization: Bearer")]
    Task<PlayerProfileDto> UpgradeUnitAsync([AliasAs("unitType")] UnitType unitType);

    [Post(ApiRoutes.MatchComplete)]
    Task CompleteMatchAsync(
        [AliasAs("matchId")] string matchId,
        [Body] MatchCompletionRequestDto request,
        [Header("X-Match-Server-Key")] string? callbackKey = null);
}
