using FarmDefenseHarvestWars.GameClient.Core.StateMachine;
using FarmDefenseHarvestWars.GameClient.Scripts.Core.StateMachine;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base.States;

public class DieState : IState
{
    private readonly BaseUnit _unit;
    private double _deathTimer = 1.0; // Default wait time before QueueFree

    public DieState(BaseUnit unit)
    {
        _unit = unit;
    }

    public void Enter()
    {
        _unit.Velocity = Vector2.Zero;

        // Disable collision and monitoring to prevent the unit from interacting while dying
        _unit.CollisionLayer = 0;
        _unit.CollisionMask = 0;

        if (_unit.HurtboxComponent != null)
        {
            _unit.HurtboxComponent.SetDeferred("monitoring", false);
            _unit.HurtboxComponent.SetDeferred("monitorable", false);
        }

        if (_unit.VisionComponent != null)
        {
            _unit.VisionComponent.SetDeferred("monitoring", false);
            _unit.VisionComponent.SetDeferred("monitorable", false);
        }

        // Try to detect the death animation length from the sprite if possible
        var animatedSprite = _unit.Visuals?.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (animatedSprite?.SpriteFrames != null && animatedSprite.SpriteFrames.HasAnimation("die"))
        {
            float speed = (float)animatedSprite.SpriteFrames.GetAnimationSpeed("die");
            int frames = animatedSprite.SpriteFrames.GetFrameCount("die");
            if (speed > 0)
            {
                _deathTimer = frames / speed;
            }
        }

        // Ensure the timer is at least a minimum value for safety
        _deathTimer = Mathf.Max(_deathTimer, 0.5f);
    }

    public void Exit()
    {
    }

    public void Update(double delta)
    {
        // Only the server (authority) should call QueueFree.
        // The node removal will be synchronized to all clients.
        if (!_unit.Multiplayer.IsServer())
            return;

        _deathTimer -= delta;
        if (_deathTimer <= 0)
        {
            _unit.QueueFree();
        }
    }

    public void PhysicsUpdate(double delta)
    {
        // No physics updates needed while dying
    }
}
