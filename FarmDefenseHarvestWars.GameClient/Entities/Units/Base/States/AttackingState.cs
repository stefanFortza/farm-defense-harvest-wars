
using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class AttackingState : IState
{
    private readonly AttackerUnit _unit;

    public AttackingState(AttackerUnit unit)
    {
        _unit = unit;
    }

    public void Enter()
    {
        GD.Print($"{_unit.Name} entered Attacking State.");
        // Set attack animation, etc. here
    }

    public void Exit()
    {
        GD.Print($"{_unit.Name} exiting Attacking State.");
        // Clean up if necessary
    }

    public void PhysicsUpdate(double delta)
    {
        // Attack logic can be handled in the unit's _PhysicsProcess
        // This state just indicates that the unit should be attacking
    }

    public void Update(double delta)
    {
        // In Attacking, we might want to check if the target is still valid or if we should switch back to moving.
        // For now, we'll just stay attacking.
    }
}