using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Entities.Projectiles;

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
        GD.Print($"{_unit.Name} entered RangedAttackState.");
        _attackTimer = 0.0;
    }

    public void Exit()
    {
        GD.Print($"{_unit.Name} exited RangedAttackState.");
    }

    public void PhysicsUpdate(double delta)
    {
        var target = _vision.GetFirstValidEnemy();

        if (target == null)
        {
            // O unitate fără viteză de deplasare (Defender static) intră obligatoriu în Idle.
            // Celelalte își reiau ciclul de patrulare/avansare (Moving).
            UnitStateEnum fallbackState = _unit.Data.Speed > 0f ? UnitStateEnum.Moving : UnitStateEnum.Idle;
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

        var projectile = _unit.Data.ProjectileScene.Instantiate<Node2D>();

        // TODO - we might want a more robust way to manage projectiles, but for now we'll just add them to a "ProjectileContainer" node in the scene if it exists, or directly to the unit's parent as a fallback.
        // Find ProjectileContainer in GameWorld
        var gameWorld = _unit.GetTree().Root.FindChild("GameWorld", true, false) as Node2D;
        var container = gameWorld?.GetNodeOrNull<Node2D>("ProjectileContainer");

        if (container != null)
        {
            container.AddChild(projectile);
        }
        else
        {
            _unit.GetParent().AddChild(projectile);
        }

        // Set position and initialize if it's a BaseProjectile
        projectile.GlobalPosition = _unit.GlobalPosition;

        if (projectile is BaseProjectile baseProj)
        {
            baseProj.Damage = _unit.Data.Damage;
            baseProj.Direction = (_unit is AttackerUnit) ? Vector2.Left : Vector2.Right;
            // Optionally set speed from Data if we add it there later
        }

        GD.Print($"{_unit.Name} fired a projectile.");
    }
}
