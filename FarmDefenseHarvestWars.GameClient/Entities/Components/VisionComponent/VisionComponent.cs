using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class VisionComponent : RayCast2D, IInitializable<(float range, Vector2 direction)>
{
    public float Range { get; set; }
    public Vector2 Direction { get; set; }

    public bool IsInitialized { get; private set; } = false;

    public void Initialize((float range, Vector2 direction) data)
    {
        if (IsInitialized) return;

        Range = data.range;
        Direction = data.direction;

        TargetPosition = Direction.Normalized() * Range;

        IsInitialized = true;
    }

    public override void _Ready()
    {
        Enabled = true; // Always on, can be toggled by states if needed
    }

    public HurtboxComponent? GetFirstValidEnemy()
    {
        ForceRaycastUpdate();

        if (!IsColliding()) return null;

        var collider = GetCollider();
        if (collider is HurtboxComponent hurtbox)
        {
            // We can add logic here to check for team/alignment if needed,
            // but the collision mask/layer should handle filtering by default.
            return hurtbox;
        }

        return null;
    }

    // Overload for returning parent BaseUnit if needed
    public Node? GetFirstValidEnemyNode()
    {
        var hurtbox = GetFirstValidEnemy();
        return hurtbox?.GetParent();
    }
}
