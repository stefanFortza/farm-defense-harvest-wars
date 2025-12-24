using Refit;
using FarmDefenseHarvestWars.Shared.Models.Auth;
using FarmDefenseHarvestWars.Shared.Models.Game;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Constants;

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
}
