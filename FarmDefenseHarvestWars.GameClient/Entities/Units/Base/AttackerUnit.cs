using Godot;

public abstract partial class AttackerUnit : BaseUnit
{
    [Export] public float Speed { get; set; } = 100.0f;
    [Export] public int Damage { get; set; } = 10;

    public override void _PhysicsProcess(double delta)
    {
        // Simple movement logic: Move Left
        // In a real server-authoritative setup, this runs on the server.
        Velocity = Vector2.Left * Speed;
        MoveAndSlide();

        // Collision handling could go here or in _Process
        HandleCollisions();
    }

    private void HandleCollisions()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            var collider = collision.GetCollider() as Node;

            if (collider is DefenderUnit defender)
            {
                // Attack logic
                Attack(defender);
            }
        }
    }

    protected virtual void Attack(DefenderUnit target)
    {
        // Placeholder attack logic
        // target.TakeDamage(Damage);
        // Stop moving while attacking?
    }
}
