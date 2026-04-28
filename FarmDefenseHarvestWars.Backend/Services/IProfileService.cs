using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IProfileService
{
    Task<PlayerProfileDto> GetProfileAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> UnlockUnitAsync(ApplicationUser user, UnitType unitType, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> UpdateAvatarAsync(ApplicationUser user, int avatarIndex, CancellationToken cancellationToken = default);
    Task<HashSet<UnitType>> GetUnlockedUnitTypesForRoleAsync(string userId, PlayerRole role, CancellationToken cancellationToken = default);
    Task<(PlayerProfileDto Profile, List<UnitUnlockDto> Rewards)> OpenChestAsync(ApplicationUser user, string chestId, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> StartUnlockChestAsync(ApplicationUser user, string chestId, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> UpgradeUnitAsync(ApplicationUser user, UnitType unitType, CancellationToken cancellationToken = default);
}
