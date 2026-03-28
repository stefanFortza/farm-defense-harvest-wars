using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmDefenseHarvestWars.Backend.Services;

public sealed class DefaultUnitUnlockService : IDefaultUnitUnlockService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitRegistryProvider _unitRegistryProvider;

    public DefaultUnitUnlockService(ApplicationDbContext db, IUnitRegistryProvider unitRegistryProvider)
    {
        _db = db;
        _unitRegistryProvider = unitRegistryProvider;
    }

    public async Task EnsureDefaultUnlocksAsync(string userId, CancellationToken cancellationToken)
    {
        List<UnitUnlock> existingUnlocks = await _db.UnitUnlocks
            .Where(unlock => unlock.UserId == userId)
            .ToListAsync(cancellationToken);

        HashSet<string> existingKeys =
        [
            .. existingUnlocks.Select(unlock => BuildKey(unlock.Role, unlock.UnitType))
        ];

        List<UnitUnlock> missingUnlocks = CreateDefaultUnlocks(userId, DateTime.UtcNow)
            .Where(unlock => !existingKeys.Contains(BuildKey(unlock.Role, unlock.UnitType)))
            .ToList();

        if (missingUnlocks.Count == 0)
        {
            return;
        }

        _db.UnitUnlocks.AddRange(missingUnlocks);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public IReadOnlyList<UnitUnlock> CreateDefaultUnlocks(string userId, DateTime unlockedAtUtc)
    {
        IReadOnlyList<UnitType> requiredDefender = _unitRegistryProvider.GetDefaultUnlockedUnitsForRole(PlayerRole.Defender);
        IReadOnlyList<UnitType> requiredAttacker = _unitRegistryProvider.GetDefaultUnlockedUnitsForRole(PlayerRole.Attacker);

        List<UnitUnlock> unlocks =
        [
            .. requiredDefender.Select(unit => new UnitUnlock
            {
                UserId = userId,
                Role = PlayerRole.Defender,
                UnitType = unit,
                UnlockedAt = unlockedAtUtc
            }),
            .. requiredAttacker.Select(unit => new UnitUnlock
            {
                UserId = userId,
                Role = PlayerRole.Attacker,
                UnitType = unit,
                UnlockedAt = unlockedAtUtc
            })
        ];

        return unlocks
            .GroupBy(unlock => BuildKey(unlock.Role, unlock.UnitType))
            .Select(group => group.First())
            .ToList();
    }

    private static string BuildKey(PlayerRole role, UnitType unitType)
    {
        return $"{role}:{unitType}";
    }
}