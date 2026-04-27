using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Entities.Projectiles;
using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class HybridRangedAttackState : IState
{
    private readonly BaseUnit _unit;
    private readonly VisionComponent _vision;
    private readonly MovementComponent _movement;

    private double _attackTimer;

    public HybridRangedAttackState(BaseUnit unit)
    {
        _unit = unit;
        _vision = unit.VisionComponent;
        _movement = unit.MovementComponent;
    }

    public void Enter()
    {
        GD.Print($"{_unit.Name} entered HybridRangedAttackState.");
        _attackTimer = 0.0;
    }

    public void Exit()
    {
        GD.Print($"{_unit.Name} exited HybridRangedAttackState.");
        _movement.Stop();
    }

    public void PhysicsUpdate(double delta)
    {
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

            if (_attackTimer <= 0)
            {
                AttackMelee(target);
                _attackTimer = GetAttackCooldown();
            }

            return;
        }

        if (distanceToTarget <= attackRange)
        {
            _movement.Stop();

            if (_attackTimer <= 0)
            {
                AttackRanged();
                _attackTimer = GetAttackCooldown();
            }

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

    private void AttackMelee(HurtboxComponent target)
    {
        if (!_unit.Multiplayer.IsServer())
        {
            return;
        }

        GD.Print($"{_unit.Name} hybrid melee attacks for {_unit.ScaledDamage} damage.");
        target.ReceiveHit(_unit.ScaledDamage);
    }

    private void AttackRanged()
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
                Direction: (_unit is AttackerUnit) ? Vector2.Left : Vector2.Right
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
