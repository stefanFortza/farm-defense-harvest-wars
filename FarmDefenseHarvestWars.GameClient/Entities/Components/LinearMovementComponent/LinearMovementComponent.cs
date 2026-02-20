using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class LinearMovementComponent : Node
{
    [Export] public float MovementSpeed { get; set; } = 100.0f;
    [Export] public bool IsMoving { get; set; } = true;

    private CharacterBody2D _parent = null!;

    public override void _Ready()
    {
        _parent = GetParent<CharacterBody2D>();
        if (_parent == null)
        {
            GD.PushError($"{Name}: LinearMovementComponent must be a child of a CharacterBody2D!");
        }
    }

    public void MoveLeft(double delta)
    {
        if (!IsMoving || _parent == null) return;

        _parent.Velocity = Vector2.Left * MovementSpeed;
        _parent.MoveAndSlide();
    }

    public void Stop()
    {
        if (_parent == null) return;
        
        _parent.Velocity = Vector2.Zero;
    }
}
