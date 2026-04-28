using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Entities.Projectiles;
using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class RangedAttackState : IState
{
    private readonly BaseUnit _unit = null!;
    private readonly VisionComponent _vision = null!;
    private double _attackTimer = 0.0;

    public RangedAttackState(BaseUnit unit)
    {
        _unit = unit;
        _vision = _unit.VisionComponent;
    }

    public void Enter()
    {
        _attackTimer = _unit.Data.AttackSpeed > 0
            ? 1.0 / _unit.Data.AttackSpeed
            : 1.0;
    }

    public void Exit()
    {
    }

    public void PhysicsUpdate(double delta)
    {
        var target = _vision.GetFirstValidEnemy();

        if (target == null)
        {
            // O unitate fără viteză de deplasare (Defender static) intră obligatoriu în Idle.
            // Celelalte își reiau ciclul de patrulare/avansare (Moving).
            UnitStateEnum fallbackState = _unit.Data.IsStatic ? UnitStateEnum.Idle : UnitStateEnum.Moving;
            _unit.StateMachine.RequestStateChange(fallbackState);
            return;
        }

        if (_attackTimer > 0)
        {
            _attackTimer -= delta;
            return;
        }

        Attack();

        // Protecție la diviziunea prin 0 în cazul în care datele sunt configurate greșit
        if (_unit.Data.AttackSpeed <= 0)
        {
            GD.PrintErr($"{_unit.Name}: AttackSpeed este 0 sau negativ!");
            _attackTimer = 1.0; // Valoare fallback de siguranță
        }
        else
        {
            _attackTimer = 1.0 / _unit.Data.AttackSpeed;
        }
    }

    public void Update(double delta)
    {
    }

    private void Attack()
    {
        if (_unit.Data.ProjectileScene == null)
        {
            GD.PrintErr($"{_unit.Name}: UnitData.ProjectileScene is missing for RangedAttackState!");
            return;
        }

        // Only the server/authority spawns projectiles; MultiplayerSpawner replicates to clients.
        if (!_unit.Multiplayer.IsServer()) return;

        var projectile = _unit.Data.ProjectileScene.Instantiate<Node2D>();

        // Use the pre-resolved ProjectileContainer injected by UnitFactory — no tree search needed.
        var container = _unit.ProjectileContainer ?? _unit.GetParent();
        container.AddChild(projectile, true);

        // Snap the projectile to the unit's X but use the lane center Y so it flies
        // along the middle of the row regardless of any vertical drift the unit may have.
        projectile.GlobalPosition = new Vector2(_unit.GlobalPosition.X, _unit.LaneCenterY);

        // Use Initialize() so HitboxComponent.DamageAmount is set after _Ready() has resolved
        // the hitbox node — avoids the stale-default timing issue.
        if (projectile is BaseProjectile baseProj)
        {
            baseProj.Initialize((
                Damage: _unit.ScaledDamage,
                Direction: _unit.GetForwardVector()
            ));
        }

    }
}
