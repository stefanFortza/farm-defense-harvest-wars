using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FarmDefenseHarvestWars.Backend.Data;

public sealed class DefaultUnitUnlockCreationInterceptor : SaveChangesInterceptor
{
    private readonly IUnitRegistryProvider _unitRegistryProvider;

    public DefaultUnitUnlockCreationInterceptor(IUnitRegistryProvider unitRegistryProvider)
    {
        _unitRegistryProvider = unitRegistryProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EnsureDefaultUnlocksForNewUsers(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnsureDefaultUnlocksForNewUsers(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void EnsureDefaultUnlocksForNewUsers(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        ApplicationUser[] newUsers = context.ChangeTracker
            .Entries<ApplicationUser>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .Where(user => !string.IsNullOrWhiteSpace(user.Id))
            .ToArray();

        if (newUsers.Length == 0)
        {
            return;
        }

        HashSet<string> existingUnlockKeys = context.ChangeTracker
            .Entries<UnitUnlock>()
            .Where(entry => entry.State != EntityState.Deleted)
            .Select(entry => BuildKey(entry.Entity.UserId, entry.Entity.Role, entry.Entity.UnitType))
            .ToHashSet(StringComparer.Ordinal);

        IReadOnlyList<UnitType> defaultDefenderUnits = _unitRegistryProvider.GetDefaultUnlockedUnitsForRole(PlayerRole.Defender);
        IReadOnlyList<UnitType> defaultAttackerUnits = _unitRegistryProvider.GetDefaultUnlockedUnitsForRole(PlayerRole.Attacker);
        DateTime unlockedAtUtc = DateTime.UtcNow;

        foreach (ApplicationUser user in newUsers)
        {
            AddMissingUnlocksForRole(context, existingUnlockKeys, user.Id, PlayerRole.Defender, defaultDefenderUnits, unlockedAtUtc);
            AddMissingUnlocksForRole(context, existingUnlockKeys, user.Id, PlayerRole.Attacker, defaultAttackerUnits, unlockedAtUtc);
        }
    }

    private void AddMissingUnlocksForRole(
        DbContext context,
        ISet<string> existingUnlockKeys,
        string userId,
        PlayerRole role,
        IReadOnlyList<UnitType> units,
        DateTime unlockedAtUtc)
    {
        foreach (UnitType unit in units)
        {
            var unitData = _unitRegistryProvider.GetUnit(unit);
            if (unitData == null || !unitData.IsDefaultUnlocked)
            {
                continue;
            }

            string key = BuildKey(userId, role, unit);
            if (!existingUnlockKeys.Add(key))
            {
                continue;
            }

            context.Add(new UnitUnlock
            {
                UserId = userId,
                Role = role,
                UnitType = unit,
                UnlockedAt = unlockedAtUtc
            });
        }
    }

    private static string BuildKey(string userId, PlayerRole role, UnitType unitType)
    {
        return $"{userId}:{role}:{unitType}";
    }
}