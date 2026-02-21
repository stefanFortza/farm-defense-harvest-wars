using Godot;
using System;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Entities.Components;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;


public partial class BaseUnit : CharacterBody2D
{
    private bool _eventsBound;

    [Signal] public delegate void HealthChangedEventHandler(int newHealth, int maxHealth);
    [Signal] public delegate void DiedEventHandler();

    [Export] public UnitData Data { get; private set; } = null!;
    [Export] public StateMachine StateMachine { get; private set; } = null!;
    [Export] public HealthComponent HealthComponent { get; private set; } = null!;
    [Export] public MovementComponent MovementComponent { get; private set; } = null!;
    [Export] public HurtboxComponent HurtboxComponent { get; private set; } = null!;
    [Export] public VisionComponent VisionComponent { get; private set; } = null!;

    public int CurrentHealth => HealthComponent?.CurrentHealth ?? 0;
    public virtual UnitType Type => Data?.Type ?? UnitType.None;
    public int MaxHealth => Data?.MaxHealth ?? 0;

    public override void _Ready()
    {
        ValidateDependencies();

        HealthComponent.HealthChanged += OnHealthChanged;
        HealthComponent.Died += Die;
        _eventsBound = true;

        HealthComponent.Initialize(MaxHealth);
        HurtboxComponent.Initialize(HealthComponent);
        MovementComponent.Initialize((this, Data.Speed));

        AddToGroup("Units");
        RegisterStates();

        if (IsMultiplayerAuthority())
        {
            StateMachine.Start(GetInitialState());
        }
    }

    public override void _ExitTree()
    {
        if (_eventsBound)
        {
            HealthComponent.HealthChanged -= OnHealthChanged;
            HealthComponent.Died -= Die;
            _eventsBound = false;
        }
    }

    private void ValidateDependencies()
    {
        this.EnsureNotNull(Data, nameof(Data));
        this.EnsureNotNull(StateMachine, nameof(StateMachine));
        this.EnsureNotNull(HealthComponent, nameof(HealthComponent));
        this.EnsureNotNull(MovementComponent, nameof(MovementComponent));
        this.EnsureNotNull(HurtboxComponent, nameof(HurtboxComponent));
        this.EnsureNotNull(VisionComponent, nameof(VisionComponent));
    }

    private void OnHealthChanged(int current, int max)
    {
        EmitSignal(SignalName.HealthChanged, current, max);
    }


    /// <summary>
    /// Registers the available states for this unit's state machine. This method can be overridden
    /// by derived classes to define custom states. It is called during the unit's initialization
    /// to set up the base idle state and any additional states required by the unit.
    /// </summary>
    protected virtual void RegisterStates()
    {
        StateMachine.RegisterState(UnitStateEnum.Idle, new IdleState(this));
    }

    protected virtual UnitStateEnum GetInitialState()
    {
        return UnitStateEnum.Idle;
    }

    protected virtual void Die()
    {
        EmitSignal(SignalName.Died);
        GD.Print($"{Type} died!");
        QueueFree();
    }
}
