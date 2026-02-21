using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;

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
        // Check for targets first
        if (_vision.GetFirstValidEnemy() != null)
        {
            _unit.StateMachine.RequestStateChange(UnitStateEnum.Attacking);
            return;
        }

        _movement.MoveLeft(delta);
    }

    public void Update(double delta)
    {
    }
}