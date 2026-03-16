using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;

public partial class DefenderUnit : BaseUnit
{
    // Defender units are static, so no movement logic here.
    // They might have an action timer for shooting or producing resources.

    [Export] public float ActionInterval { get; set; } = 1.0f;
    protected Timer _actionTimer = null!;

    public override void _Ready()
    {
        base._Ready();

        VisionComponent.Initialize((Data.AttackRange, Vector2.Right)); // Defenders look to the right by default

        // Get Timer from Scene
        _actionTimer = GetNode<Timer>("ActionTimer");
        _actionTimer.WaitTime = ActionInterval;
        _actionTimer.OneShot = false;
        _actionTimer.Timeout += OnActionTimerTimeout;
        _actionTimer.Start();
    }

    // This is called by the base class's _Ready after validating dependencies
    protected override void RegisterStates()
    {
        base.RegisterStates();
    }

    protected override UnitStateEnum GetInitialState()
    {
        return UnitStateEnum.Idle; // Defenders start idle and perform actions via timer
    }

    protected override void Die()
    {
        // Stop timer before dying
        _actionTimer.Stop();
        base.Die();
    }

    protected virtual void OnActionTimerTimeout()
    {
        // Override this in subclasses (e.g., Cow blocks, Chicken shoots)
    }
}