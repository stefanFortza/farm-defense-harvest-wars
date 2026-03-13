using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Gameplay.Environment.Decorations;

/// <summary>
/// Base class for all animated decorations (torches, campfires, flags, etc.).
/// Handles desynchronization of animations to avoid "clone effects".
/// </summary>
public partial class AnimatedDecoration : Node2D
{
    [Export] public AnimatedSprite2D? AnimSprite { get; set; }
    
    [Export] public bool RandomizeStartFrame { get; set; } = true;
    [Export] public bool RandomizePlaybackSpeed { get; set; } = true;

    public override void _Ready()
    {
        if (AnimSprite == null)
        {
            GD.PushWarning($"AnimatedDecoration: AnimSprite not assigned on '{Name}'. Attempting to find local AnimatedSprite2D child.");
            AnimSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            
            if (AnimSprite == null)
            {
                GD.PushError($"AnimatedDecoration: No AnimatedSprite2D found for '{Name}'. Logic aborted.");
                return;
            }
        }

        // Ensure animation starts
        AnimSprite.Play();

        // Desynchronization logic
        if (AnimSprite.SpriteFrames != null)
        {
            StringName currentAnim = AnimSprite.Animation;
            int frameCount = AnimSprite.SpriteFrames.GetFrameCount(currentAnim);

            if (RandomizeStartFrame && frameCount > 1)
            {
                // Set a random start frame
                AnimSprite.Frame = (int)(GD.Randi() % (uint)frameCount);
            }

            if (RandomizePlaybackSpeed)
            {
                // Vary speed slightly (0.9x to 1.1x) so they drift apart over time
                AnimSprite.SpeedScale = (float)GD.RandRange(0.9, 1.1);
            }
        }
    }
}
