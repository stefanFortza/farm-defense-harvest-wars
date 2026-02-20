using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Entities.Components;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;


public partial class BaseUnit : CharacterBody2D
{

    [Signal] public delegate void HealthChangedEventHandler(int newHealth, int maxHealth);
    [Signal] public delegate void DiedEventHandler();

    [Export] public UnitData Data { get; set; } = null!;
    [Export] protected StateMachine StateMachine { get; set; } = null!;

    // The new HealthComponent
    [Export] public HealthComponent HealthComponent { get; private set; } = null!;

    public int CurrentHealth => HealthComponent?.CurrentHealth ?? 0;
    public virtual UnitType Type => Data?.Type ?? UnitType.None;
    public int MaxHealth => Data?.MaxHealth ?? 0;

    public override void _Ready()
    {
        if (Data == null)
        {
            GD.PushError($"[BaseUnit] {Name}: UnitData is missing!");
            return;
        }

        StateMachine ??= GetNodeOrNull<StateMachine>("StateMachine");
        if (StateMachine == null)
        {
            GD.PushError($"[BaseUnit] {Name}: StateMachine node is missing!");
            return;
        }

        HealthComponent ??= GetNodeOrNull<HealthComponent>("Components/HealthComponent");
        if (HealthComponent == null)
        {
            GD.PushError($"[BaseUnit] {Name}: HealthComponent is missing!");
            return;
        }

        HealthComponent.HealthChanged += OnHealthChanged;
        HealthComponent.Died += Die;

        HealthComponent.Initialize(MaxHealth);

        AddToGroup("Units");
        RegisterStates();

        if (IsMultiplayerAuthority())
        {
            StateMachine.Start(GetInitialState());
        }
    }

    public override void _ExitTree()
    {
        if (HealthComponent != null)
        {
            HealthComponent.HealthChanged -= OnHealthChanged;
            HealthComponent.Died -= Die;
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        EmitSignal(SignalName.HealthChanged, current, max);
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
        // Logic is now delegated to the component
        HealthComponent.TakeDamage(amount);

        GD.Print($"{Type} took {amount} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected virtual void Die()
    {
        EmitSignal(SignalName.Died);
        GD.Print($"{Type} died!");
        QueueFree();
    }
}
