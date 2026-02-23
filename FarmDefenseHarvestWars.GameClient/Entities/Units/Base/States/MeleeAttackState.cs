using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class MeleeAttackState : IState
{
    private readonly BaseUnit _unit;
    private readonly VisionComponent _vision;
    private double _attackTimer = 0.0;

    public MeleeAttackState(BaseUnit unit)
    {
        _unit = unit;
        _vision = _unit.VisionComponent;
    }

    public void Enter()
    {
        GD.Print($"{_unit.Name} entered MeleeAttackState.");
        _attackTimer = 1.0 / _unit.Data.AttackSpeed; // Start with cooldown? or immediate?
        // Let's start ready to attack if the timer is 0.
        _attackTimer = 0.0;
    }

    public void Exit()
    {
        GD.Print($"{_unit.Name} exited MeleeAttackState.");
    }

    public void PhysicsUpdate(double delta)
    {

        var target = _vision.GetFirstValidEnemy();
        if (target == null)
        {
            UnitStateEnum fallbackState = _unit.Data.Speed > 0f ? UnitStateEnum.Moving : UnitStateEnum.Idle;
            _unit.StateMachine.RequestStateChange(fallbackState);
            return;
        }

        if (_attackTimer > 0)
        {
            _attackTimer -= delta;
            return;
        }

        Attack(target);
        _attackTimer = 1.0 / _unit.Data.AttackSpeed;
    }

    public void Update(double delta)
    {
    }

    private void Attack(HurtboxComponent target)
    {
        GD.Print($"{_unit.Name} melee attacks target for {_unit.Data.Damage} damage.");
        target.ReceiveHit(_unit.Data.Damage);
        // Trigger attack animation here via _unit.Visuals if available
    }
}
