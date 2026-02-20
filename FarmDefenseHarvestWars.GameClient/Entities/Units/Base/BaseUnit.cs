using Godot;
using System;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;


public partial class BaseUnit : CharacterBody2D
{
    [Export] public UnitData Data { get; set; } = null!;
    [Export] protected StateMachine StateMachine { get; set; } = null!;

    // Dynamic stats (change during gameplay)
    public int CurrentHealth { get; protected set; }

    // Read-only properties fetching data from the shared Resource
    public virtual UnitType Type => Data?.Type ?? UnitType.None;
    public int MaxHealth => Data?.MaxHealth ?? 100;

    [Signal] public delegate void HealthChangedEventHandler(int newHealth, int maxHealth);
    [Signal] public delegate void DiedEventHandler();
    public override void _Ready()
    {
        if (Data == null)
        {
            GD.PrintErr($"[BaseUnit] {Name} is missing UnitData!");
        }

        CurrentHealth = MaxHealth;
        AddToGroup("Units");

        StateMachine ??= GetNode<StateMachine>("StateMachine");

        RegisterStates();

        if (IsMultiplayerAuthority())
        {
            StateMachine.Start(GetInitialState());
        }
    }


    protected virtual void RegisterStates()
    {
        StateMachine.RegisterState(UnitStateEnum.Idle, new IdleState(this));
    }

    protected virtual UnitStateEnum GetInitialState()
    {
        return UnitStateEnum.Idle;
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
