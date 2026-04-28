using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;

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
            Level = 1,
            Fragments = 0,
            UnlockedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        return await BuildPlayerProfileAsync(user, cancellationToken);
    }

    public async Task<PlayerProfileDto> UpdateAvatarAsync(ApplicationUser user, int avatarIndex, CancellationToken cancellationToken = default)
    {
        if (avatarIndex < 0 || avatarIndex > 7)
        {
            throw new ArgumentException("Avatar index must be between 0 and 7.");
        }

        user.AvatarIndex = avatarIndex;
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

    public async Task<(PlayerProfileDto Profile, List<UnitUnlockDto> Rewards)> OpenChestAsync(ApplicationUser user, string chestId, CancellationToken cancellationToken = default)
    {
        var chestsJson = string.IsNullOrWhiteSpace(user.ChestsJson) ? "[]" : user.ChestsJson;
        var chests = JsonSerializer.Deserialize<List<ChestDto>>(chestsJson) ?? new();
        var chest = chests.FirstOrDefault(c => c.Id == chestId);

        if (chest == null)
        {
            throw new InvalidOperationException("Chest not found.");
        }

        if (!chest.UnlockStartTime.HasValue)
        {
            throw new InvalidOperationException("Chest is not being unlocked.");
        }

        var unlockTimeElapsed = DateTime.UtcNow - chest.UnlockStartTime.Value;
        if (unlockTimeElapsed.TotalSeconds < chest.UnlockDurationSeconds)
        {
            throw new InvalidOperationException("Chest is still unlocking.");
        }

        chests.Remove(chest);
        user.ChestsJson = JsonSerializer.Serialize(chests);

        // Logic for rewards: give fragments for 1-2 random units that the player has unlocked
        var unlocks = await _db.UnitUnlocks
            .Where(u => u.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (!unlocks.Any())
        {
            // Fallback if somehow no units are unlocked (shouldn't happen due to EnsureDefaultUnlocksAsync)
            await _defaultUnitUnlockService.EnsureDefaultUnlocksAsync(user.Id, cancellationToken);
            unlocks = await _db.UnitUnlocks
                .Where(u => u.UserId == user.Id)
                .ToListAsync(cancellationToken);
        }

        var random = new Random();
        var rewards = new List<UnitUnlockDto>();

        // Give rewards for 1 or 2 units
        int rewardCount = random.Next(1, 3);
        var shuffledUnlocks = unlocks.OrderBy(x => random.Next()).Take(rewardCount).ToList();

        foreach (var unlock in shuffledUnlocks)
        {
            int fragmentAmount = random.Next(5, 15);
            unlock.Fragments += fragmentAmount;

            rewards.Add(new UnitUnlockDto
            {
                UnitType = unlock.UnitType,
                Level = unlock.Level,
                Fragments = fragmentAmount // Here we send the amount found, not total
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        var profile = await BuildPlayerProfileAsync(user, cancellationToken);

        return (profile, rewards);
    }

    public async Task<PlayerProfileDto> StartUnlockChestAsync(ApplicationUser user, string chestId, CancellationToken cancellationToken = default)
    {
        var chestsJson = string.IsNullOrWhiteSpace(user.ChestsJson) ? "[]" : user.ChestsJson;
        var chests = JsonSerializer.Deserialize<List<ChestDto>>(chestsJson) ?? new();
        var chest = chests.FirstOrDefault(c => c.Id == chestId);

        if (chest == null)
        {
            throw new InvalidOperationException("Chest not found.");
        }

        if (chest.UnlockStartTime.HasValue)
        {
            throw new InvalidOperationException("Chest is already unlocking.");
        }

        // Rule: Only one chest can be unlocked at a time
        if (chests.Any(c => c.UnlockStartTime.HasValue))
        {
            // Optional: check if any is already finished. If not, throw.
            // For now, simple: only one allowed in the unlocking state.
            throw new InvalidOperationException("Another chest is already unlocking.");
        }

        chest.UnlockStartTime = DateTime.UtcNow;
        user.ChestsJson = JsonSerializer.Serialize(chests);

        await _db.SaveChangesAsync(cancellationToken);

        return await BuildPlayerProfileAsync(user, cancellationToken);
    }

    public async Task<PlayerProfileDto> UpgradeUnitAsync(ApplicationUser user, UnitType unitType, CancellationToken cancellationToken = default)
    {
        var unlock = await _db.UnitUnlocks
            .FirstOrDefaultAsync(u => u.UserId == user.Id && u.UnitType == unitType, cancellationToken);

        if (unlock == null)
        {
            throw new InvalidOperationException("Unit not unlocked.");
        }

        int fragmentsRequired = unlock.Level * 10;
        int goldCost = unlock.Level * 100;

        if (unlock.Fragments < fragmentsRequired)
        {
            throw new InvalidOperationException($"Not enough fragments. Required: {fragmentsRequired}, available: {unlock.Fragments}.");
        }

        if (user.Gold < goldCost)
        {
            throw new InvalidOperationException($"Not enough gold. Required: {goldCost}, available: {user.Gold}.");
        }

        user.Gold -= goldCost;
        unlock.Fragments -= fragmentsRequired;
        unlock.Level++;

        await _db.SaveChangesAsync(cancellationToken);

        return await BuildPlayerProfileAsync(user, cancellationToken);
    }

    private async Task<PlayerProfileDto> BuildPlayerProfileAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        PlayerUnlockedUnitsDto unlockedUnits = await GetUnlockedUnitsDtoAsync(user.Id, cancellationToken);
        var chestsJson = string.IsNullOrWhiteSpace(user.ChestsJson) ? "[]" : user.ChestsJson;
        var chests = JsonSerializer.Deserialize<List<ChestDto>>(chestsJson) ?? new();

        return new PlayerProfileDto
        {
            Email = user.Email!,
            Gold = user.Gold,
            Level = user.Level,
            Xp = user.Xp,
            AvatarIndex = user.AvatarIndex,
            UnlockedUnits = unlockedUnits,
            Chests = chests
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
                .Select(unlock => new UnitUnlockDto
                {
                    UnitType = unlock.UnitType,
                    Level = unlock.Level,
                    Fragments = unlock.Fragments
                })
                .OrderBy(u => u.UnitType)
                .ToList(),
            AttackerUnits = unlocks
                .Where(unlock => unlock.Role == PlayerRole.Attacker)
                .Select(unlock => new UnitUnlockDto
                {
                    UnitType = unlock.UnitType,
                    Level = unlock.Level,
                    Fragments = unlock.Fragments
                })
                .OrderBy(u => u.UnitType)
                .ToList()
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
