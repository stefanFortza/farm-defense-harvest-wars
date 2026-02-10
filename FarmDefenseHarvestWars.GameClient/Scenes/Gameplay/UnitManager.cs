using Godot;
using System;
using System.Collections.Generic;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;

public partial class UnitManager : Node
{
    // Dictionary to hold packed scenes for units
    private Dictionary<UnitType, PackedScene> _unitScenes = new Dictionary<UnitType, PackedScene>();

    public override void _Ready()
    {
        // Load unit scenes (Hardcoded for now, could be dynamic)
        LoadUnitScene(UnitType.Cow, "res://Entities/Units/Defenders/CowUnit.tscn");
        LoadUnitScene(UnitType.Wolf, "res://Entities/Units/Enemies/WolfUnit.tscn");
    }

    private void LoadUnitScene(UnitType type, string path)
    {
        var scene = GD.Load<PackedScene>(path);
        if (scene != null)
        {
            _unitScenes[type] = scene;
        }
        else
        {
            GD.PrintErr($"Failed to load unit scene: {path}");
        }
    }

    public BaseUnit SpawnUnit(UnitType unitType, Vector2 position, Node parent)
    {
        if (_unitScenes.TryGetValue(unitType, out var scene))
        {
            var unitInstance = scene.Instantiate<BaseUnit>();
            unitInstance.Position = position;
            parent.AddChild(unitInstance);
            GD.Print($"Spawned {unitType} at {position}");
            return unitInstance;
        }

        GD.PrintErr($"Unit type not found: {unitType}");
        return null;
    }
}
