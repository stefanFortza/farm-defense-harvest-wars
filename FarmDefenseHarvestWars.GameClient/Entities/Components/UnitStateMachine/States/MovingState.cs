using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;
using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class MovingState : IState
{
    private readonly BaseUnit _unit;
    private readonly MovementComponent _movement = null!;
    private readonly VisionComponent _vision = null!;

    public MovingState(BaseUnit unit)
    {
        _unit = unit;
        _movement = _unit.MovementComponent;
        _vision = _unit.VisionComponent;
    }

    public void Enter()
    {
        GD.Print($"{_unit.Name} entered WalkState.");
        _movement.IsMoving = true;
    }

    public void Exit()
    {
        GD.Print($"{_unit.Name} exited WalkState.");
        _movement.Stop();
    }

    public void PhysicsUpdate(double delta)
    {
        // 1. Check for targets in front
        if (_vision.GetFirstValidEnemy() != null)
        {
            _unit.StateMachine.RequestStateChange(UnitStateEnum.Attacking);
            return;
        }

        // 2. Check for targets in back - ONLY FOR DEFENDERS
        if (_unit.Data.Role == PlayerRole.Defender && _unit.SecondaryVisionComponent != null && _unit.SecondaryVisionComponent.GetFirstValidEnemy() != null)
        {
            GD.Print($"{_unit.Name} (Defender) detected enemy in BACK vision while moving. Flipping!");
            _unit.Flip();
            _unit.StateMachine.RequestStateChange(UnitStateEnum.Attacking);
            return;
        }

        _movement.MoveForward(delta);
    }

    public void Update(double delta)
    {
    }
}