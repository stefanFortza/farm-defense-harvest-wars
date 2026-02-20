
using Godot;
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;


public class MovingState : IState
{
    private readonly AttackerUnit _unit;

    public MovingState(AttackerUnit unit)
    {
        _unit = unit;
    }

    public void Enter()
    {
        GD.Print($"{_unit.Name} entered WalkState.");
        // Set animation, direction, etc. here
    }

    public void Exit()
    {
        GD.Print($"{_unit.Name} exited WalkState.");
        // Clean up if necessary
    }

    public void PhysicsUpdate(double delta)
    {
        GD.Print($"{_unit.Name} is walking with speed {_unit.Speed}.");
        // throw new System.NotImplementedException();
        _unit.Velocity = Vector2.Left * _unit.Speed;
        _unit.MoveAndSlide();
    }

    public void Update(double delta)
    {
        // Movement logic can be handled in the unit's _PhysicsProcess
        // This state just indicates that the unit should be moving
    }
}