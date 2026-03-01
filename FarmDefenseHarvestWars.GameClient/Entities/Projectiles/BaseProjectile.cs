using Godot;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

namespace FarmDefenseHarvestWars.GameClient.Entities.Projectiles;

public partial class BaseProjectile : Node2D, IInitializable<(int Damage, Vector2 Direction)>
{
    [Export] public HitboxComponent HitboxComponent { get; set; } = null!;
    [Export] public float Speed { get; set; } = 10f;
    [Export] public Vector2 Direction { get; set; } = Vector2.Left;
    [Export] public int Damage { get; set; } = 10;
    [Export] public float MaxLifetime { get; set; } = 5.0f;


    public bool IsInitialized { get; private set; } = false;

    public override void _Ready()
    {
        this.EnsureNotNull(HitboxComponent, nameof(HitboxComponent));
        // Wire up the signal; damage is applied via Initialize() after AddChild.
        HitboxComponent.AreaEntered += OnHitboxAreaEntered;

        // Auto-destroy after MaxLifetime seconds so missed projectiles don't linger.
        GetTree().CreateTimer(MaxLifetime).Timeout += QueueFree;
    }

    /// <summary>
    /// Initializes gameplay parameters for this projectile. Call this immediately after
    /// adding the projectile to the scene tree so HitboxComponent.DamageAmount is set
    /// correctly (avoiding the stale-default timing issue that existed before).
    /// </summary>
    public void Initialize((int Damage, Vector2 Direction) data)
    {
        if (IsInitialized) return;

        Damage = data.Damage;
        Direction = data.Direction;

        HitboxComponent.DamageAmount = data.Damage;

        IsInitialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Only the authority should drive movement; replication handles client-side position.
        if (!IsMultiplayerAuthority()) return;

        Position += Direction * Speed * (float)delta;
    }

    private void OnHitboxAreaEntered(Area2D area)
    {
        if (area is HurtboxComponent hurtbox)
        {
            OnHit(hurtbox);
        }
    }

    /// <summary>
    /// Called when the projectile overlaps a HurtboxComponent. Override in subclasses to
    /// implement special behavior (piercing, AoE, homing, etc.). The base implementation
    /// destroys the projectile on first hit.
    /// </summary>
    protected virtual void OnHit(HurtboxComponent target)
    {
        QueueFree();
    }
}
