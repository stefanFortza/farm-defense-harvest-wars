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
