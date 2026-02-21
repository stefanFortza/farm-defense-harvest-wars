using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class HealthComponent : Node, IInitializable<int>
{
    [Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void DiedEventHandler();

    public int MaxHealth { get; set; }
    public int CurrentHealth { get; private set; }

    public bool IsInitialized { get; private set; } = false;

    public void Initialize(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        IsInitialized = true;
    }

    public void TakeDamage(int amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth = Math.Max(0, CurrentHealth - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            EmitSignal(SignalName.Died);
        }
    }

    public void Heal(int amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public void SetMaxHealth(int amount, bool resetHealth = true)
    {
        MaxHealth = amount;
        if (resetHealth)
        {
            CurrentHealth = MaxHealth;
            EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        }
    }
}
