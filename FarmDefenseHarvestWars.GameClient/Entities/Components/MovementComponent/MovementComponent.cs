using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class MovementComponent : Node, IInitializable<(CharacterBody2D Parent, float Speed)>
{
    [Export] public bool IsMoving { get; set; } = true;
    [Export] private CharacterBody2D _parent = null!;
    public float MovementSpeed { get; set; }

    public bool IsInitialized { get; private set; } = false;

    public void Initialize((CharacterBody2D Parent, float Speed) data)
    {
        if (IsInitialized)
            return;
        _parent = data.Parent;
        MovementSpeed = data.Speed;
        IsInitialized = true;
    }

    public override void _Ready()
    {
        this.EnsureNotNull(_parent, nameof(_parent));
    }

    public void MoveLeft(double delta)
    {
        if (GodotObject.IsInstanceValid(_parent) == false)
        {
            GD.PrintErr("MovementComponent: Parent is not valid.");
            return;
        }

        if (!IsMoving) return;

        _parent.Velocity = Vector2.Left * MovementSpeed;
        _parent.MoveAndSlide();
    }

    public void Stop()
    {
        if (_parent == null) return;

        _parent.Velocity = Vector2.Zero;
    }

}
