using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using Refit;
using Godot;

public static class DeckSelectionLogic
{
    public const int MaxCards = 5;

    private static readonly object SaveSync = new();
    private static readonly Dictionary<PlayerRole, List<UnitType>> PendingDeckByRole = [];
    private static readonly Dictionary<PlayerRole, long> LatestVersionByRole = [];
    private static readonly HashSet<PlayerRole> IsProcessingRole = [];

    public static bool TryGetAssignedRole(out PlayerRole role)
    {
        var state = GameState.Instance;
        if (state == null || !state.HasAssignedRole)
        {
            role = default;
            return false;
        }

        role = state.AssignedRole!.Value;
        return true;
    }

    public static List<UnitType> GetDeckForRole(PlayerRole role)
    {
        var result = new List<UnitType>(MaxCards);
        var state = GameState.Instance;
        if (state?.CurrentDeck == null)
        {
            return result;
        }

        var source = role == PlayerRole.Attacker
            ? state.CurrentDeck.AttackerDeck
            : state.CurrentDeck.DefenderDeck;

        foreach (var unitType in source)
        {
            if (!IsRoleCompatible(unitType, role))
            {
                continue;
            }

            if (result.Contains(unitType))
            {
                continue;
            }

            result.Add(unitType);
            if (result.Count >= MaxCards)
            {
                break;
            }
        }

        return result;
    }

    public static void SubmitDeckSaveForRole(PlayerRole role, IReadOnlyList<UnitType> units)
    {
        var normalized = NormalizeDeckForRole(role, units);
        bool shouldStartLoop;

        lock (SaveSync)
        {
            PendingDeckByRole[role] = normalized;

            long currentVersion = LatestVersionByRole.TryGetValue(role, out var version)
                ? version
                : 0;
            LatestVersionByRole[role] = currentVersion + 1;

            shouldStartLoop = IsProcessingRole.Add(role);
        }

        if (shouldStartLoop)
        {
            _ = ProcessSaveLoop(role);
        }
    }

    private static async Task ProcessSaveLoop(PlayerRole role)
    {
        while (true)
        {
            List<UnitType>? requestedDeck;
            long requestVersion;

            lock (SaveSync)
            {
                if (!PendingDeckByRole.TryGetValue(role, out requestedDeck))
                {
                    IsProcessingRole.Remove(role);
                    GameState.Instance?.SetDeckSaveInProgress(role, false);
                    return;
                }

                PendingDeckByRole.Remove(role);
                requestVersion = LatestVersionByRole[role];
            }

            GameState.Instance?.SetDeckSaveInProgress(role, true);

            try
            {
                var updatedDeck = await NetworkBootstrap.Instance.Menu.UpdateDeckForRoleAsync(role, requestedDeck);

                if (!IsLatestVersion(role, requestVersion))
                {
                    continue;
                }

                GameState.Instance?.SetDeckForRole(role, updatedDeck.Units);
                GameState.Instance?.NotifyDeckSaveResult(role, true, string.Empty);
            }
            catch (ApiException ex)
            {
                GD.PrintErr($"Failed to save deck for role {role}: {ex.Message}");

                if (IsLatestVersion(role, requestVersion))
                {
                    GameState.Instance?.NotifyDeckSaveResult(role, false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Unexpected error while saving deck for role {role}: {ex.Message}");

                if (IsLatestVersion(role, requestVersion))
                {
                    GameState.Instance?.NotifyDeckSaveResult(role, false, "Unexpected deck save error.");
                }
            }
        }
    }

    private static bool IsLatestVersion(PlayerRole role, long version)
    {
        lock (SaveSync)
        {
            return LatestVersionByRole.TryGetValue(role, out var latestVersion)
                && latestVersion == version;
        }
    }

    private static List<UnitType> NormalizeDeckForRole(PlayerRole role, IReadOnlyList<UnitType> units)
    {
        var normalized = new List<UnitType>(MaxCards);

        foreach (var unit in units)
        {
            if (normalized.Count >= MaxCards)
            {
                break;
            }

            if (!IsRoleCompatible(unit, role))
            {
                continue;
            }

            if (normalized.Contains(unit))
            {
                continue;
            }

            normalized.Add(unit);
        }

        return normalized;
    }

    public static List<UnitData> GetCompatibleUnits(UnitRegistry registry, PlayerRole role)
    {
        var result = new List<UnitData>();
        foreach (var unit in registry.AllUnits)
        {
            if (unit == null)
            {
                continue;
            }

            if (!IsRoleCompatible(unit.Type, role))
            {
                continue;
            }

            result.Add(unit);
        }

        return result;
    }

    public static bool IsRoleCompatible(UnitType unitType, PlayerRole role)
    {
        if (role == PlayerRole.Attacker)
        {
            return unitType == UnitType.Skeleton;
        }

        if (role == PlayerRole.Defender)
        {
            return unitType != UnitType.Skeleton;
        }

        return false;
    }
}
