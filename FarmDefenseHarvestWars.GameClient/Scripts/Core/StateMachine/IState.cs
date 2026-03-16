namespace FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;

/// <summary>
/// Defines the contract for a state within the state machine.
/// Ensures a strict separation between lifecycle events, visual updates, and authoritative physics logic.
/// </summary>
public interface IState
{
	/// <summary>
	/// Called once when the state machine transitions into this state.
	/// Use this for initialization, starting animations, or triggering server-side entry logic.
	/// </summary>
	void Enter();

	/// <summary>
	/// Called once when the state machine transitions out of this state.
	/// Use this for cleanup operations, resetting variables, or stopping state-specific effects.
	/// </summary>
	void Exit();

	/// <summary>
	/// Called every frame (_Process). 
	/// Strictly reserved for client-side visual logic, UI updates, timers, and non-authoritative mechanics.
	/// </summary>
	/// <param name="delta">The time elapsed since the last frame.</param>
	void Update(double delta);

	/// <summary>
	/// Called every physics frame (_PhysicsProcess). 
	/// Strictly reserved for authoritative server logic, physics calculations, collisions, and movement.
	/// </summary>
	/// <param name="delta">The time elapsed since the last physics frame.</param>
	void PhysicsUpdate(double delta);
}