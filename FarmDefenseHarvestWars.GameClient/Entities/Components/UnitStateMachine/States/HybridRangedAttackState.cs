using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Entities.Projectiles;
using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class HybridRangedAttackState : IAttackState
{
    private readonly BaseUnit _unit;
    private readonly VisionComponent _vision;
    private readonly MovementComponent _movement;

    private double _attackTimer;
    private double _windUpTimer = -1.0;
    private HurtboxComponent? _currentTarget;

    public double CooldownTimer => _attackTimer;
    public double WindUpTimer => _windUpTimer;

    public HybridRangedAttackState(BaseUnit unit)
    {
        _unit = unit;
        _vision = unit.VisionComponent;
        _movement = unit.MovementComponent;
    }

    public void Enter()
    {
        _attackTimer = 0.0;
        _windUpTimer = -1.0;
        _currentTarget = null;
    }

    public void Exit()
    {
        _movement.Stop();
    }

    public void PhysicsUpdate(double delta)
    {
        // 1. Handle Active Melee Wind-up
        if (_windUpTimer > 0)
        {
            _windUpTimer -= delta;
            if (_windUpTimer <= 0)
            {
                ApplyMeleeHit();
            }
            return;
        }

        var target = _vision.GetFirstValidEnemy();
        if (target == null)
        {
            UnitStateEnum fallbackState = _unit.Data.IsStatic ? UnitStateEnum.Idle : UnitStateEnum.Moving;
            _unit.StateMachine.RequestStateChange(fallbackState);
            return;
        }

        if (_attackTimer > 0)
        {
            _attackTimer -= delta;
            return;
        }

        var distanceToTarget = _unit.GlobalPosition.DistanceTo(target.GlobalPosition);

        float attackRange = _unit.Data.AttackRange;
        float optimalRange = _unit.Data.OptimalRange > 0
            ? Mathf.Min(_unit.Data.OptimalRange, attackRange)
            : attackRange;
        float meleeRange = _unit.Data.MeleeRange > 0
            ? Mathf.Min(_unit.Data.MeleeRange, optimalRange)
            : 0f;

        if (distanceToTarget <= meleeRange)
        {
            _movement.Stop();
            StartMeleeSequence(target);
            return;
        }

        if (distanceToTarget <= attackRange)
        {
            _movement.Stop();
            StartRangedSequence();
            return;
        }

        if (distanceToTarget > optimalRange)
        {
            _movement.IsMoving = true;
            _movement.MoveLeft(delta);
        }
        else
        {
            _movement.Stop();
        }
    }

    public void Update(double delta)
    {
    }

    public void StartAttackSequence()
    {
        StartRangedSequence();
    }

    private void StartMeleeSequence(HurtboxComponent target)
    {
        if (!_unit.Multiplayer.IsServer()) return;

        double attackCycle = 1.0 / Mathf.Max(0.1f, _unit.Data.AttackSpeed);
        _currentTarget = target;
        _windUpTimer = attackCycle * 0.5; // Always 50% of the cycle
        _attackTimer = GetAttackCooldown();

        _unit.Rpc(nameof(BaseUnit.NotifyAttackStartedRPC));
    }

    private void ApplyMeleeHit()
    {
        if (!_unit.Multiplayer.IsServer()) return;
        _windUpTimer = -1.0;

        if (GodotObject.IsInstanceValid(_currentTarget))
        {
            GD.Print($"{_unit.Name} hybrid melee hits for {_unit.ScaledDamage} damage.");
            _currentTarget!.ReceiveHit(_unit.ScaledDamage);
            _unit.Rpc(nameof(BaseUnit.NotifyHitImpactRPC), _currentTarget.GetParent().GetPath());
        }

        _currentTarget = null;
    }

    private void StartRangedSequence()
    {
        if (_unit.Data.ProjectileScene == null)
        {
            GD.PrintErr($"{_unit.Name}: UnitData.ProjectileScene is missing for HybridRangedAttackState!");
            return;
        }

        if (!_unit.Multiplayer.IsServer())
        {
            return;
        }

        _attackTimer = GetAttackCooldown();

        _unit.Rpc(nameof(BaseUnit.NotifyAttackStartedRPC));
        _unit.Rpc(nameof(BaseUnit.NotifyHitImpactRPC), new NodePath());

        var projectile = _unit.Data.ProjectileScene.Instantiate<Node2D>();

        var container = _unit.ProjectileContainer ?? _unit.GetParent<Node2D>();
        if (container == null)
        {
            GD.PrintErr($"{_unit.Name}: No projectile container available for HybridRangedAttackState.");
            return;
        }

        container.AddChild(projectile, true);

        projectile.GlobalPosition = new Vector2(_unit.GlobalPosition.X, _unit.LaneCenterY);

        if (projectile is BaseProjectile baseProjectile)
        {
            baseProjectile.Initialize((
                Damage: _unit.ScaledDamage,
                Direction: (_unit is AttackerUnit) ? Vector2.Left : Vector2.Right,
                IsFromAttacker: _unit is AttackerUnit
            ));
        }

        GD.Print($"{_unit.Name} fired a hybrid projectile.");
    }

    private double GetAttackCooldown()
    {
        if (_unit.Data.AttackSpeed <= 0)
        {
            GD.PrintErr($"{_unit.Name}: AttackSpeed is 0 or negative. Using fallback cooldown.");
            return 1.0;
        }

        return 1.0 / _unit.Data.AttackSpeed;
    }
}
