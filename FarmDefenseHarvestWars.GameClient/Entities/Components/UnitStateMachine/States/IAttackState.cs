using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

/// <summary>
/// Marker interface for all unit attack states.
/// </summary>
public interface IAttackState : IState
{
    /// <summary>
    /// Starts the attack sequence (wind-up and cooldown).
    /// </summary>
    void StartAttackSequence();

    /// <summary>
    /// The current wind-up timer value. Positive if winding up.
    /// </summary>
    double WindUpTimer { get; }

    /// <summary>
    /// The current cooldown timer value. Positive if on cooldown.
    /// </summary>
    double CooldownTimer { get; }
}
