using System.Collections.Generic;
using FarmDefenseHarvestWars.Shared.Enums;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Data;

public static class UnitStatsRegistry
{
    private static readonly Dictionary<UnitType, UnitData> _stats = new();
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        if (_isInitialized) return;

        // Load all unit data resources
        // Note: In a production scenario, you might want to scan the directory
        LoadResource(UnitType.Cow, "res://Resources/UnitStats/CowData.tres");
        LoadResource(UnitType.Wolf, "res://Resources/UnitStats/WolfData.tres");

        _isInitialized = true;
        GD.Print("UnitStatsRegistry: Initialized with Godot Resources.");
    }

    private static void LoadResource(UnitType type, string path)
    {
        var data = GD.Load<UnitData>(path);
        if (data != null)
        {
            _stats[type] = data;
        }
        else
        {
            GD.PrintErr($"UnitStatsRegistry: Failed to load resource at {path}");
        }
    }

    public static UnitData Get(UnitType type)
    {
        if (!_isInitialized) Initialize();

        if (_stats.TryGetValue(type, out UnitData? value))
            return value;

        GD.PrintErr($"UnitStatsRegistry: Stats not found for {type}. Using fallback.");

        // Fallback object
        return new UnitData
        {
            Cost = 9999,
            MaxHealth = 1,
            Damage = 0,
            AttackRange = 0,
            AttackSpeed = 1
        };
    }
}