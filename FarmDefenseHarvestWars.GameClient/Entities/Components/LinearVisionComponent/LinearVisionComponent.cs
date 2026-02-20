using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Entities.Components;

public partial class LinearVisionComponent : RayCast2D
{
    [Export] public float Range { get; set; } = 50.0f;
    [Export] public Vector2 Direction { get; set; } = Vector2.Left;

    public override void _Ready()
    {
        // Ensure RayCast2D properties are set properly
        TargetPosition = Direction.Normalized() * Range;
        Enabled = true; // Always on, can be toggled by states if needed
    }

    public override void _PhysicsProcess(double delta)
    {
        // Dynamic target update if Range or Direction change during runtime
        TargetPosition = Direction.Normalized() * Range;
    }

    public HurtboxComponent? GetFirstValidEnemy()
    {
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
