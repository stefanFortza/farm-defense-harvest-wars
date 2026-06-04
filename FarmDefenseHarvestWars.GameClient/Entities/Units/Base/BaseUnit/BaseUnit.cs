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

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void NetDespawn()
    {
        // Force cleanup on all clients, including reconnected ones
        QueueFree();
    }

    [Signal] public delegate void HealthChangedEventHandler(int newHealth, int maxHealth);
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void AttackStartedEventHandler();
    [Signal] public delegate void HitImpactEventHandler(NodePath targetPath);

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void NotifyAttackStartedRPC()
    {
        EmitSignal(SignalName.AttackStarted);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void NotifyHitImpactRPC(NodePath targetPath)
    {
        EmitSignal(SignalName.HitImpact, targetPath);
    }

    [Export] public UnitData Data { get; private set; } = null!;
    [Export] public UnitStateMachine StateMachine { get; private set; } = null!;
    [Export] public HealthComponent HealthComponent { get; private set; } = null!;
    [Export] public MovementComponent MovementComponent { get; private set; } = null!;
    [Export] public HurtboxComponent HurtboxComponent { get; private set; } = null!;
    [Export] public VisionComponent VisionComponent { get; private set; } = null!;
    [Export] public VisionComponent? SecondaryVisionComponent { get; private set; }
    [Export] public Node2D? Visuals { get; private set; }

    private int _facingDirection = 1;
    [Export]
    public int FacingDirection
    {
        get => _facingDirection;
        set
        {
            _facingDirection = value;
            ApplyFacing();
        }
    }

    public int CurrentHealth => HealthComponent?.CurrentHealth ?? 0;
    public virtual UnitType Type => Data?.Type ?? UnitType.None;
    public int MaxHealth => Data?.MaxHealth ?? 0;

    [Export] public int Level { get; private set; } = 1;

    public int ScaledMaxHealth => (int)(MaxHealth * (1 + (Level - 1) * 0.1f));
    public int ScaledDamage => (int)((Data?.Damage ?? 0) * (1 + (Level - 1) * 0.1f));

    public void SetLevel(int level)
    {
        Level = level;
        if (HealthComponent != null && IsInsideTree())
        {
            HealthComponent.Initialize(ScaledMaxHealth);
        }
    }


    public Node2D? ProjectileContainer { get; set; }

    /// <summary>
    /// The Y world-coordinate of the center of the lane this unit was spawned in.
    /// Set by UnitFactory at spawn time so projectiles fly along the lane center.
    /// </summary>
    public float LaneCenterY { get; set; }

    public override void _Ready()
    {
        ValidateDependencies();

        HealthComponent.HealthChanged += OnHealthChanged;
        HealthComponent.Died += Die;
        
        _eventsBound = true;

        HealthComponent.Initialize(ScaledMaxHealth);
        HurtboxComponent.Initialize(HealthComponent);
        MovementComponent.Initialize((this, Data.Speed));

        AddToGroup("Units");
        RegisterStates();

        // Initial flip state
        ApplyFacing();

        // if (IsMultiplayerAuthority())
        // {
        StateMachine.Start(GetInitialState());
        // }
    }

    public void Flip()
    {
        FacingDirection *= -1;
        ApplyFacing();
    }

    public Vector2 GetForwardVector()
    {
        // Attackers are base-oriented Left, others Right
        Vector2 baseDir = (this is AttackerUnit) ? Vector2.Left : Vector2.Right;
        return baseDir * FacingDirection;
    }

    private void ApplyFacing()
    {
        if (Visuals != null)
        {
            Vector2 scale = Visuals.Scale;
            scale.X = Math.Abs(scale.X) * FacingDirection;
            Visuals.Scale = scale;
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
        StateMachine.RegisterState(UnitStateEnum.Dying, new DieState(this));

        // Register attack state based on data
        if (Data?.ProjectileScene != null)
        {
            StateMachine.RegisterState(UnitStateEnum.Attacking, new RangedAttackState(this));
        }
        else
        {
            StateMachine.RegisterState(UnitStateEnum.Attacking, new MeleeAttackState(this));
        }
    }

    protected virtual UnitStateEnum GetInitialState()
    {
        return UnitStateEnum.Idle;
    }

    protected virtual void Die()
    {
        EmitSignal(SignalName.Died);
        if (Multiplayer.IsServer())
        {
            StateMachine.RequestStateChange(UnitStateEnum.Dying);
        }
    }
}
