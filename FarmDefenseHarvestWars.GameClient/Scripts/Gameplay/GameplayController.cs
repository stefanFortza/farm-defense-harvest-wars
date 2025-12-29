using Godot;
using System;
using FarmDefenseHarvestWars.Shared.Enums;

public partial class GameplayController : Node2D
{
    // References
    private UnitManager _unitManager = null!;
    private GridManager _gridManager = null!;

    public override void _Ready()
    {
        GD.Print("Gameplay Scene Initialized");

        _unitManager = GetNode<UnitManager>("UnitManager");
        _gridManager = GetNode<GridManager>("FarmMap");

        // Test Spawn (Delayed to ensure everything is ready)
        GetTree().CreateTimer(1.0f).Timeout += () =>
        {
            // Spawn a Cow at grid coordinates (3, 2)
            SpawnUnitOnGrid(UnitType.Cow, new Vector2I(3, 2));
            GD.Print("Spawned Cow at (3,2)");

            // Spawn a Wolf at grid coordinates (10, 2) - Attacker Lane
            SpawnUnitOnGrid(UnitType.Wolf, new Vector2I(10, 2));
        };
    }

    private void SpawnUnitOnGrid(UnitType unitType, Vector2I gridPos)
    {
        if (_gridManager == null || _unitManager == null) return;

        Vector2 worldPos = _gridManager.GetWorldPosition(gridPos);
        _unitManager.SpawnUnit(unitType, worldPos, this);
    }

    public override void _Process(double delta)
    {
    }
}
