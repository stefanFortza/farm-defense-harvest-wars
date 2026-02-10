using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class IdleState : IState
{
	private readonly BaseUnit _unit;

	public IdleState(BaseUnit unit)
	{
		_unit = unit;
	}

	public void Enter()
	{
		GD.Print($"{_unit.Type} entered Idle State.");
		_unit.Velocity = Vector2.Zero; // Stop movement
	}

	public void Exit()
	{
		GD.Print($"{_unit.Type} exiting Idle State.");
	}

	public void Update(double delta)
	{
		// In Idle, we might want to check for nearby enemies or conditions to transition out of idle.
		// For now, we'll just stay idle.
	}

	public void PhysicsUpdate(double delta)
	{
		// No physics updates needed in idle.
	}
}