using Refit;
using FarmDefenseHarvestWars.Shared.Models.Auth;
using FarmDefenseHarvestWars.Shared.Models.Game;
using System.Threading.Tasks;

namespace FarmDefenseHarvestWars.Shared.API;

public interface IGameApi
{
    [Post("/register")]
    Task RegisterAsync([Body] RegisterRequestDto request);

    [Post("/login")]
    Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto request);

    [Get("/api/game/profile")]
    [Headers("Authorization: Bearer")]
    Task<PlayerProfileDto> GetProfileAsync();
}
