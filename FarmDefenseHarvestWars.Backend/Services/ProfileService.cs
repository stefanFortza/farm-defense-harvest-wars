using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.EntityFrameworkCore;

namespace FarmDefenseHarvestWars.Backend.Services;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly IDefaultUnitUnlockService _defaultUnitUnlockService;
    private readonly IUnitRegistryProvider _unitRegistryProvider;

    public ProfileService(
        ApplicationDbContext db,
        IDefaultUnitUnlockService defaultUnitUnlockService,
        IUnitRegistryProvider unitRegistryProvider)
    {
        _db = db;
        _defaultUnitUnlockService = defaultUnitUnlockService;
        _unitRegistryProvider = unitRegistryProvider;
    }

    public async Task<PlayerProfileDto> GetProfileAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        await _defaultUnitUnlockService.EnsureDefaultUnlocksAsync(user.Id, cancellationToken);
        return await BuildPlayerProfileAsync(user, cancellationToken);
    }

    public async Task<PlayerProfileDto> UnlockUnitAsync(ApplicationUser user, UnitType unitType, CancellationToken cancellationToken = default)
    {
        if (unitType == UnitType.None)
        {
            throw new ArgumentException("Unit type is invalid.");
        }

        UnitDataDto? unitData = _unitRegistryProvider.GetUnit(unitType);
        if (unitData == null)
        {
            throw new ArgumentException("Unknown unit.");
        }

        if (unitData.IsDefaultUnlocked)
        {
            throw new InvalidOperationException("Unit is unlocked by default.");
        }

        PlayerRole? role = ResolveRoleForUnit(unitType, unitData.Role);
        if (!role.HasValue)
        {
            throw new InvalidOperationException("Could not resolve unit role.");
        }

        bool alreadyUnlocked = await _db.UnitUnlocks.AnyAsync(
            unlock => unlock.UserId == user.Id && unlock.Role == role.Value && unlock.UnitType == unitType,
            cancellationToken);
        if (alreadyUnlocked)
        {
            throw new InvalidOperationException("Unit already unlocked.");
        }

        if (user.Gold < unitData.UnlockCost)
        {
            throw new InvalidOperationException($"Not enough gold. Required: {unitData.UnlockCost}, available: {user.Gold}.");
        }

        user.Gold -= unitData.UnlockCost;
        _db.UnitUnlocks.Add(new UnitUnlock
        {
            UserId = user.Id,
            Role = role.Value,
            UnitType = unitType,
            UnlockedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return await BuildPlayerProfileAsync(user, cancellationToken);
    }

    public async Task<HashSet<UnitType>> GetUnlockedUnitTypesForRoleAsync(string userId, PlayerRole role, CancellationToken cancellationToken = default)
    {
        List<UnitType> unlockedUnits = await _db.UnitUnlocks
            .AsNoTracking()
            .Where(unlock => unlock.UserId == userId && unlock.Role == role)
            .Select(unlock => unlock.UnitType)
            .ToListAsync(cancellationToken);

        return unlockedUnits.ToHashSet();
    }

    private async Task<PlayerProfileDto> BuildPlayerProfileAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        PlayerUnlockedUnitsDto unlockedUnits = await GetUnlockedUnitsDtoAsync(user.Id, cancellationToken);

        return new PlayerProfileDto
        {
            Email = user.Email!,
            Gold = user.Gold,
            Level = user.Level,
            Xp = user.Xp,
            UnlockedUnits = unlockedUnits
        };
    }

    private async Task<PlayerUnlockedUnitsDto> GetUnlockedUnitsDtoAsync(string userId, CancellationToken cancellationToken)
    {
        List<UnitUnlock> unlocks = await _db.UnitUnlocks
            .AsNoTracking()
            .Where(unlock => unlock.UserId == userId)
            .ToListAsync(cancellationToken);

        return new PlayerUnlockedUnitsDto
        {
            DefenderUnits = unlocks
                .Where(unlock => unlock.Role == PlayerRole.Defender)
                .Select(unlock => unlock.UnitType)
                .Distinct()
                .OrderBy(unit => unit)
                .ToArray(),
            AttackerUnits = unlocks
                .Where(unlock => unlock.Role == PlayerRole.Attacker)
                .Select(unlock => unlock.UnitType)
                .Distinct()
                .OrderBy(unit => unit)
                .ToArray()
        };
    }

    private PlayerRole? ResolveRoleForUnit(UnitType unitType, PlayerRole? unitRole)
    {
        if (unitRole.HasValue)
        {
            return unitRole.Value;
        }

        bool compatibleWithDefender = _unitRegistryProvider.IsRoleCompatible(unitType, PlayerRole.Defender);
        bool compatibleWithAttacker = _unitRegistryProvider.IsRoleCompatible(unitType, PlayerRole.Attacker);

        if (compatibleWithDefender && !compatibleWithAttacker)
        {
            return PlayerRole.Defender;
        }

        if (compatibleWithAttacker && !compatibleWithDefender)
        {
            return PlayerRole.Attacker;
        }

        return null;
    }
}
