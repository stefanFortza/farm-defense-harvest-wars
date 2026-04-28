using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;
using FarmDefenseHarvestWars.Shared.Enums;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class IdleState : IState
{
	private readonly BaseUnit _unit;
	private readonly VisionComponent _vision;

	public IdleState(BaseUnit unit)
	{
		_unit = unit;
		_vision = _unit.VisionComponent;
	}

	public void Enter()
	{
		_unit.Velocity = Vector2.Zero; // Stop movement
	}

	public void Exit()
	{
	}

	public void Update(double delta)
	{
		// In Idle, we might want to check for nearby enemies or conditions to transition out of idle.
		// For now, we'll just stay idle.
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
			_unit.Flip();
			_unit.StateMachine.RequestStateChange(UnitStateEnum.Attacking);
			return;
		}
	}
}