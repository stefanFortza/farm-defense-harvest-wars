using Godot;
using System;

public partial class GameplayController : Node2D
{
    // References
    private UnitManager _unitManager;

    public override void _Ready()
    {
        GD.Print("Gameplay Scene Initialized");

        _unitManager = GetNode<UnitManager>("UnitManager");

        // Test Spawn (Delayed to ensure everything is ready)
        GetTree().CreateTimer(1.0f).Timeout += () =>
        {
            _unitManager.SpawnUnit("Cow", new Vector2(200, 200), this);
        };
    }

    public override void _Process(double delta)
    {
    }
}
