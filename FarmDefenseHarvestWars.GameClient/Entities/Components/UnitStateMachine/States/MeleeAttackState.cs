using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;
using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class MeleeAttackState : IState
{
    private readonly BaseUnit _unit;
    private readonly VisionComponent _vision;
    private double _cooldownTimer = 0.0;
    private double _windUpTimer = -1.0;
    private HurtboxComponent? _currentTarget;

    public MeleeAttackState(BaseUnit unit)
    {
        _unit = unit;
        _vision = _unit.VisionComponent;
    }

    public void Enter()
    {
        _cooldownTimer = 0.0;
        _windUpTimer = -1.0;
        _currentTarget = null;
    }

    public void Exit()
    {
    }

    public void PhysicsUpdate(double delta)
    {
        // 1. Handle Active Wind-up (Damage Delay)
        if (_windUpTimer > 0)
        {
            _windUpTimer -= delta;
            if (_windUpTimer <= 0)
            {
                ApplyHit();
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
        HurtboxComponent? target = null;
        if (_unit.Data.Role == PlayerRole.Defender && _unit.SecondaryVisionComponent != null)
        {
            target = _unit.SecondaryVisionComponent.GetFirstValidEnemy();
            if (target != null) _unit.Flip();
        }

        if (target == null)
        {
            target = _vision.GetFirstValidEnemy();
        }

        if (target == null)
        {
            UnitStateEnum fallbackState = _unit.Data.IsStatic ? UnitStateEnum.Idle : UnitStateEnum.Moving;
            _unit.StateMachine.RequestStateChange(fallbackState);
            return;
        }

        StartAttackSequence(target);
    }

    public void Update(double delta)
    {
    }

    private void StartAttackSequence(HurtboxComponent target)
    {
        if (!_unit.Multiplayer.IsServer()) return;

        double attackCycle = 1.0 / _unit.Data.AttackSpeed;
        _currentTarget = target;
        _windUpTimer = attackCycle * 0.5; // Always 50% of the animation
        _cooldownTimer = attackCycle;

        // Notify clients to START the animation
        _unit.Rpc(nameof(BaseUnit.NotifyAttackStartedRPC));
    }

    private void ApplyHit()
    {
        if (!_unit.Multiplayer.IsServer()) return;

        _windUpTimer = -1.0;

        // "Guaranteed" means we apply damage if target is still valid, even if unit was dying
        // (but since this is a state, unit must be alive for this to run).
        if (GodotObject.IsInstanceValid(_currentTarget))
        {
            _currentTarget!.ReceiveHit(_unit.ScaledDamage);
            _unit.Rpc(nameof(BaseUnit.NotifyHitImpactRPC), _currentTarget.GetParent().GetPath());
        }
        
        _currentTarget = null;
    }
}
