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

    public static async Task<bool> SaveDeckForRole(PlayerRole role, IReadOnlyList<UnitType> units)
    {
        var trimmed = new List<UnitType>(MaxCards);

        foreach (var unit in units)
        {
            if (trimmed.Count >= MaxCards)
            {
                break;
            }

            if (!IsRoleCompatible(unit, role))
            {
                continue;
            }

            if (trimmed.Contains(unit))
            {
                continue;
            }

            trimmed.Add(unit);
        }

        try
        {
            await NetworkBootstrap.Instance.Menu.UpdateDeckForRoleAsync(role, trimmed);
            return true;
        }
        catch (ApiException ex)
        {
            GD.PrintErr($"Failed to save deck for role {role}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Unexpected error while saving deck for role {role}: {ex.Message}");
            return false;
        }
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
