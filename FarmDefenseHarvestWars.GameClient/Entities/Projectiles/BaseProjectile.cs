using Godot;
using FarmDefenseHarvestWars.GameClient.Entities.Components;

namespace FarmDefenseHarvestWars.GameClient.Entities.Projectiles;

public partial class BaseProjectile : Area2D
{
    [Export] public float Speed { get; set; } = 300.0f;
    [Export] public Vector2 Direction { get; set; } = Vector2.Left;
    [Export] public int Damage { get; set; } = 10;

    private HitboxComponent _hitbox = null!;

    public override void _Ready()
    {
        _hitbox = GetNodeOrNull<HitboxComponent>("HitboxComponent");
        if (_hitbox != null)
        {
            _hitbox.DamageAmount = Damage;
            _hitbox.AreaEntered += OnHitboxAreaEntered;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += Direction * Speed * (float)delta;
    }

    private void OnHitboxAreaEntered(Area2D area)
    {
        // Check if we hit a hurtbox (HitboxComponent already does the damage)
        if (area is HurtboxComponent)
        {
            QueueFree();
        }
    }
}
