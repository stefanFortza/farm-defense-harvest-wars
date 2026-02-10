using Godot;
using System;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using System.Runtime.CompilerServices;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;


public abstract partial class BaseUnit : CharacterBody2D
{
    [Export] protected StateMachine StateMachine { get; set; } = null!;

    [Export] public int MaxHealth { get; set; } = 100;

    // Synced via MultiplayerSynchronizer in the future
    public int CurrentHealth { get; protected set; }

    public abstract UnitType Type { get; }

    [Signal] public delegate void HealthChangedEventHandler(int newHealth, int maxHealth);
    [Signal] public delegate void DiedEventHandler();

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        AddToGroup("Units");

        // Register states
        StateMachine.RegisterState(UnitStateEnum.Idle, new IdleState(this));
    }

    // Server-side logic
    public virtual void TakeDamage(int amount)
    {
        // In a real scenario, check if IsMultiplayerAuthority()
        CurrentHealth -= amount;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        GD.Print($"{Type} took {amount} damage. HP: {CurrentHealth}/{MaxHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        EmitSignal(SignalName.Died);
        GD.Print($"{Type} died!");
        QueueFree();
    }
}
