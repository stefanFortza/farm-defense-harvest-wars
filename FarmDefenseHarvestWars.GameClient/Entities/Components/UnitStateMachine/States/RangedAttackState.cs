using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Entities.Projectiles;
using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class RangedAttackState : IAttackState
{
    private readonly BaseUnit _unit = null!;
    private readonly VisionComponent _vision = null!;
    private double _cooldownTimer = 0.0;
    private double _windUpTimer = -1.0;

    public double CooldownTimer => _cooldownTimer;
    public double WindUpTimer => _windUpTimer;

    public RangedAttackState(BaseUnit unit)
    {
        _unit = unit;
        _vision = _unit.VisionComponent;
    }

    public void Enter()
    {
        _cooldownTimer = 0.0;
        _windUpTimer = -1.0;
    }

    public void Exit()
    {
    }

    public void PhysicsUpdate(double delta)
    {
        // 1. Handle Active Wind-up (Fire Delay)
        if (_windUpTimer > 0)
        {
            _windUpTimer -= delta;
            if (_windUpTimer <= 0)
            {
                ApplyFire();
            }
            return;
        }

        // 2. Cooldown wait
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= delta;
            return;
        }

        // 3. Find target to start new attack
        var target = _vision.GetFirstValidEnemy();

        if (target == null)
        {
            UnitStateEnum fallbackState = _unit.Data.IsStatic ? UnitStateEnum.Idle : UnitStateEnum.Moving;
            _unit.StateMachine.RequestStateChange(fallbackState);
            return;
        }

        StartAttackSequence();
    }

    public void Update(double delta)
    {
    }

    public void StartAttackSequence()
    {
        if (!_unit.Multiplayer.IsServer()) return;

        double attackCycle = 1.0 / Mathf.Max(0.1f, _unit.Data.AttackSpeed);
        _windUpTimer = attackCycle * 0.5; // Always 50%
        _cooldownTimer = attackCycle;

        // Notify clients to START the animation
        _unit.Rpc(nameof(BaseUnit.NotifyAttackStartedRPC));
    }

    private void ApplyFire()
    {
        if (!_unit.Multiplayer.IsServer()) return;

        _windUpTimer = -1.0;

        if (_unit.Data.ProjectileScene == null)
        {
            GD.PrintErr($"{_unit.Name}: UnitData.ProjectileScene is missing for RangedAttackState!");
            return;
        }

        _unit.Rpc(nameof(BaseUnit.NotifyHitImpactRPC), new NodePath());

        var projectile = _unit.Data.ProjectileScene.Instantiate<Node2D>();

        // Use the pre-resolved ProjectileContainer injected by UnitFactory — no tree search needed.
        var container = _unit.ProjectileContainer ?? _unit.GetParent();
        container.AddChild(projectile, true);

        // Snap the projectile to the unit's X but use the lane center Y
        projectile.GlobalPosition = new Vector2(_unit.GlobalPosition.X, _unit.LaneCenterY);

        if (projectile is BaseProjectile baseProj)
        {
            baseProj.Initialize((
                Damage: _unit.ScaledDamage,
                Direction: _unit.GetForwardVector(),
                IsFromAttacker: _unit is AttackerUnit
            ));
        }
    }
}
