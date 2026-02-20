using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class HurtboxComponent : Area2D
{
    [Export] public HealthComponent HealthComponent { get; set; } = null!;

    public void ReceiveHit(int damage)
    {
        if (HealthComponent == null)
        {
            GD.PrintErr($"{Name}: HurtboxComponent lacks a HealthComponent reference!");
            return;
        }

        HealthComponent.TakeDamage(damage);
    }
}
