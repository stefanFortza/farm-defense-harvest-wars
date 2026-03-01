using Godot;

namespace FarmDefenseHarvestWars.GameClient.Entities.Projectiles;

/// <summary>
/// Optional per-projectile behavior flags. Attach to a concrete projectile scene as an
/// exported resource, or leave null to use the plain BaseProjectile default behavior.
///
/// Extension points (not yet wired up, reserved for future projectile variants):
///   - Piercing   : projectile passes through enemies (no QueueFree on hit until PierceCount runs out)
///   - AoeRadius  : on hit, damage all HurtboxComponents within this radius
///   - Homing     : each physics frame, steer toward the nearest enemy hurtbox
/// </summary>
[GlobalClass]
public partial class ProjectileConfig : Resource
{
    /// <summary>Whether the projectile continues through enemies instead of stopping.</summary>
    [Export] public bool Piercing { get; set; } = false;

    /// <summary>How many enemies a piercing projectile can penetrate before it stops.</summary>
    [Export] public int PierceCount { get; set; } = 1;

    /// <summary>
    /// Radius (in pixels) for AoE splash damage on impact. 0 disables AoE.
    /// </summary>
    [Export] public float AoeRadius { get; set; } = 0f;

    /// <summary>Whether the projectile steers toward the nearest enemy each frame.</summary>
    [Export] public bool Homing { get; set; } = false;
}
