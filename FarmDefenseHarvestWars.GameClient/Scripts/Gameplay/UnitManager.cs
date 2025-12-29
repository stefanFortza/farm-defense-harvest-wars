using Godot;
using System;
using System.Collections.Generic;

public partial class UnitManager : Node
{
    // Dictionary to hold packed scenes for units
    private Dictionary<string, PackedScene> _unitScenes = new Dictionary<string, PackedScene>();

    public override void _Ready()
    {
        // Load unit scenes (Hardcoded for now, could be dynamic)
        LoadUnitScene("Cow", "res://Entities/Units/CowUnit.tscn");
        // LoadUnitScene("Chicken", "res://Entities/Units/ChickenUnit.tscn");
    }

    private void LoadUnitScene(string key, string path)
    {
        var scene = GD.Load<PackedScene>(path);
        if (scene != null)
        {
            _unitScenes[key] = scene;
        }
        else
        {
            GD.PrintErr($"Failed to load unit scene: {path}");
        }
    }

    public BaseUnit SpawnUnit(string unitType, Vector2 position, Node parent)
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
