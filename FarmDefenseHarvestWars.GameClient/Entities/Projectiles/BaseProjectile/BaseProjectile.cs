using Godot;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

namespace FarmDefenseHarvestWars.GameClient.Entities.Projectiles;

public partial class BaseProjectile : Node2D, IInitializable<(int Damage, Vector2 Direction, bool IsFromAttacker)>
{
    [Export] public HitboxComponent HitboxComponent { get; set; } = null!;
    [Export] public Sprite2D Sprite2D { get; set; } = null!;
    [Export] public ProjectileConfig? Config { get; set; }
    [Export] public float Speed { get; set; } = 100f;
    [Export] public Vector2 Direction { get; set; } = Vector2.Left;
    [Export] public int Damage { get; set; } = 10;
    [Export] public float MaxLifetime { get; set; } = 5.0f;
    [Export] public bool IsFromAttacker { get; set; }

    [Export] public Vector2 TargetPosition { get; set; }
    [Export] public float InterpolationSpeed { get; set; } = 25.0f;


    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void NetDespawn()
    {
        QueueFree();
    }

    public bool IsInitialized { get; private set; } = false;

    public override void _Ready()
    {
        this.EnsureNotNull(HitboxComponent, nameof(HitboxComponent));
        this.EnsureNotNull(Sprite2D, nameof(Sprite2D));

        // Wire up hit detection on all instances (Server for damage, Client for visuals)
        HitboxComponent.AreaEntered += OnHitboxAreaEntered;

        // Initialize target position to avoid jumping on spawn
        TargetPosition = GlobalPosition;

        if (!IsMultiplayerAuthority())
            return;

        // Auto-destroy after MaxLifetime seconds so missed projectiles don't linger.
        GetTree().CreateTimer(MaxLifetime).Timeout += () => 
        {
            if (IsInsideTree()) Rpc(nameof(NetDespawn));
        };
    }

    /// <summary>
    /// Initializes gameplay parameters for this projectile. Call this immediately after
    /// adding the projectile to the scene tree so HitboxComponent.DamageAmount is set
    /// correctly (avoiding the stale-default timing issue that existed before).
    /// </summary>
    public void Initialize((int Damage, Vector2 Direction, bool IsFromAttacker) data)
    {
        if (IsInitialized) return;

        Damage = data.Damage;
        Direction = data.Direction;
        IsFromAttacker = data.IsFromAttacker;

        HitboxComponent.DamageAmount = data.Damage;

        IsInitialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority())
        {
            Position += Direction * Speed * (float)delta;
            TargetPosition = GlobalPosition;
        }
        else
        {
            // Smoothly interpolate towards the server position
            GlobalPosition = GlobalPosition.Lerp(TargetPosition, (float)(InterpolationSpeed * delta));
        }
    }

    private void OnHitboxAreaEntered(Area2D area)
    {
        if (area is HurtboxComponent hurtbox)
        {
            if (IsMultiplayerAuthority())
            {
                OnHit(hurtbox);
            }
        }
    }

    /// <summary>
    /// Called when the projectile overlaps a HurtboxComponent. Override in subclasses to
    /// implement special behavior (piercing, AoE, homing, etc.). The base implementation
    /// destroys the projectile on first hit.
    /// </summary>
    protected virtual void OnHit(HurtboxComponent target)
    {
        ApplyAreaDamage(target);
        
        // Broadcast visuals to all peers
        Rpc(nameof(PlayImpactVisualsRPC), target.GlobalPosition, IsFromAttacker);
        
        Rpc(nameof(NetDespawn));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void PlayImpactVisualsRPC(Vector2 impactPoint, bool isFromAttacker)
    {
        float aoeRadius = Config?.AoeRadius ?? 0f;
        
        // Show impact effect for everyone, even non-AOE (use small radius)
        float visualRadius = aoeRadius > 0 ? aoeRadius : 6.0f;

        var effect = new AoeExplosionEffect
        {
            Radius = visualRadius,
            Color = GetImpactColor(),
            GlobalPosition = impactPoint
        };
        
        // Add to parent to stay in the world coordinate system (e.g., ProjectileContainer)
        if (GetParent() != null)
        {
            GetParent().AddChild(effect);
        }
        else
        {
            GetTree().Root.AddChild(effect);
        }

        // Trigger hurt animation on hit targets for visual feedback
        TriggerVisualHurtAtPoint(impactPoint, aoeRadius, isFromAttacker);
    }

    protected virtual Color GetImpactColor() => Colors.White;

    private void TriggerVisualHurtAtPoint(Vector2 impactPoint, float radius, bool isFromAttacker)
    {
        // 1. Check Units
        foreach (Node node in GetTree().GetNodesInGroup("Units"))
        {
            if (node is not BaseUnit unit) continue;
            
            // Only play hurt for enemies
            bool isUnitAttacker = unit is AttackerUnit;
            if (isUnitAttacker == isFromAttacker) continue;

            TryPlayHurtOnUnit(unit, impactPoint, radius);
        }

        // 2. Check DefenderBase if from attacker
        if (isFromAttacker)
        {
            foreach (Node node in GetTree().GetNodesInGroup("DefenderBase"))
            {
                if (node is not DefenderBase defBase) continue;
                
                float distance = defBase.GlobalPosition.DistanceTo(impactPoint);
                if (radius > 0)
                {
                    if (distance <= radius) defBase.PlayHitEffect();
                }
                else if (distance < 24f)
                {
                    defBase.PlayHitEffect();
                }
            }
        }
    }

    private void TryPlayHurtOnUnit(BaseUnit unit, Vector2 impactPoint, float radius)
    {
        float distance = unit.GlobalPosition.DistanceTo(impactPoint);
        // If radius is 0 (direct hit), we check if the unit contains the impact point roughly
        if (radius > 0)
        {
            if (distance <= radius)
            {
                unit.Visuals?.GetNodeOrNull<UnitVisualsComponent>("UnitVisualsComponent")?.PlayHurtAnimation();
            }
        }
        else
        {
            // Direct hit fallback - if it's very close to unit center
            if (distance < 16f) 
            {
                unit.Visuals?.GetNodeOrNull<UnitVisualsComponent>("UnitVisualsComponent")?.PlayHurtAnimation();
            }
        }
    }

    private void ApplyAreaDamage(HurtboxComponent directHitTarget)
    {
        float aoeRadius = Config?.AoeRadius ?? 0f;
        if (aoeRadius <= 0f || !IsMultiplayerAuthority())
        {
            return;
        }

        if (!GodotObject.IsInstanceValid(directHitTarget))
        {
            return;
        }

        Vector2 impactPoint = directHitTarget.GlobalPosition;

        // 1. Apply to Units
        foreach (Node node in GetTree().GetNodesInGroup("Units"))
        {
            if (node is not BaseUnit unit) continue;

            // Only damage enemies
            bool isUnitAttacker = unit is AttackerUnit;
            if (isUnitAttacker == IsFromAttacker) continue;

            var hurtbox = unit.HurtboxComponent;
            if (hurtbox == null || !GodotObject.IsInstanceValid(hurtbox) || hurtbox == directHitTarget) continue;

            if (hurtbox.GlobalPosition.DistanceTo(impactPoint) <= aoeRadius)
            {
                hurtbox.ReceiveHit(Damage);
            }
        }

        // 2. Apply to DefenderBase if from attacker
        if (IsFromAttacker)
        {
            foreach (Node node in GetTree().GetNodesInGroup("DefenderBase"))
            {
                if (node is not DefenderBase defBase) continue;
                
                var hurtbox = defBase.HurtboxComponent;
                if (hurtbox == null || !GodotObject.IsInstanceValid(hurtbox) || hurtbox == directHitTarget) continue;

                if (hurtbox.GlobalPosition.DistanceTo(impactPoint) <= aoeRadius)
                {
                    hurtbox.ReceiveHit(Damage);
                }
            }
        }
    }
}
