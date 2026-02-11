using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Units.Base;

public abstract partial class AttackerUnit : BaseUnit
{
    // These now pull from Data if available, or use defaults
    public float Speed => Data != null ? 150.0f : 100.0f; // Could be added to UnitData if needed
    public int Damage => Data?.Damage ?? 10;
    public float AttackSpeed => Data?.AttackSpeed ?? 1.0f;

    private double _attackCooldown = 0.0;
    private DefenderUnit? _currentTarget = null;

    public override void _PhysicsProcess(double delta)
    {
        // Cooldown management
        if (_attackCooldown > 0)
        {
            _attackCooldown -= delta;
        }

        // Combat State
        if (IsInstanceValid(_currentTarget))
        {
            // We have a target, stop moving and attack
            if (_attackCooldown <= 0)
            {
                Attack(_currentTarget);
                _attackCooldown = 1.0 / AttackSpeed;
            }
        }
        else
        {
            // No target, move forward
            _currentTarget = null; // Clear invalid reference
            Velocity = Vector2.Left * Speed;
            MoveAndSlide();

            // Check for new collisions
            HandleCollisions();
        }
    }

    private void HandleCollisions()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            var collider = collision.GetCollider() as Node;

            if (collider is DefenderUnit defender)
            {
                // Found a target!
                _currentTarget = defender;
                break; // Engage the first one we hit
            }
        }
    }

    protected virtual void Attack(DefenderUnit target)
    {
        GD.Print($"{Type} attacks {target.Type} for {Damage} damage!");
        target.TakeDamage(Damage);
    }
}
