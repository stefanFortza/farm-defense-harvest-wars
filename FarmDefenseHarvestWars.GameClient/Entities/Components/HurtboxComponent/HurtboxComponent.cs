using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class HurtboxComponent : Area2D, IInitializable<HealthComponent>
{
    [Export] public HealthComponent HealthComponent { get; set; } = null!;

    public bool IsInitialized { get; private set; } = false;

    public void Initialize(HealthComponent healthComponent)
    {
        HealthComponent = healthComponent;
        IsInitialized = true;
    }

    public override void _Ready()
    {
        ValidateDependencies();
    }

    public void ReceiveHit(int damage)
    {
        if (!Multiplayer.IsServer()) return;
        
        GD.Print($"[HurtboxComponent] {Name} on {GetParent().Name} received hit for {damage} damage.");

        if (HealthComponent == null)
        {
            GD.PrintErr($"{Name}: HurtboxComponent lacks a HealthComponent reference!");
            return;
        }

        HealthComponent.TakeDamage(damage);
    }

    private void ValidateDependencies()
    {
        Node? parentNode = GetNodeOrNull<Node>("../..");
        this.EnsureNotNull(parentNode, "Parent node at ../..");

        CollisionShape2D? collisionShape = null;

        foreach (Node child in GetChildren())
        {
            if (child is CollisionShape2D shape)
            {
                collisionShape = shape;
                break;
            }
        }

        parentNode!.EnsureNotNull(collisionShape, $"CollisionShape2D child (required by {Name})");
    }
}
