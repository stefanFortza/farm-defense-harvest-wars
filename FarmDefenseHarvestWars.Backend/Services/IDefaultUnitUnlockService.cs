using FarmDefenseHarvestWars.Backend.Models;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IDefaultUnitUnlockService
{
    Task EnsureDefaultUnlocksAsync(string userId, CancellationToken cancellationToken);
    IReadOnlyList<UnitUnlock> CreateDefaultUnlocks(string userId, DateTime unlockedAtUtc);
}