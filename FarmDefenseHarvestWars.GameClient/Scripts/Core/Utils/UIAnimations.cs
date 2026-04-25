using System;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Core.Utils;

/// <summary>
/// Safe wrapper for UIAnimation singleton autoload.
/// Provides robust C#-to-GDScript interop for UI animations with graceful fallback.
/// </summary>
public static class UIAnimations
{
    private const string UIAnimationPath = "/root/UIAnimation";

    /// <summary>
    /// Animates a node's scale to a specific target value.
    /// </summary>
    public static bool TryAnimateScale(Control node, Vector2 targetScale, double duration)
    {
        if (node == null)
        {
            return false;
        }

        if (!TryGetUIAnimation(out var uiAnimation))
        {
            return false;
        }

        try
        {
            // Set pivot offset to center for proper scaling
            node.PivotOffset = new Vector2(node.Size.X / 2, node.Size.Y / 2);

            // Create tween directly and call from the autoload context
            Tween tween = node.CreateTween();
            tween.SetTrans(Tween.TransitionType.Back);
            tween.SetEase(Tween.EaseType.Out);
            tween.TweenProperty(node, "scale", targetScale, duration);

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"UIAnimation scalar animate failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Animates a node's scale to 1.04x (hover scale for TabButton).
    /// </summary>
    public static bool TryAnimateScaleUp(Control node, double duration = 0.2)
    {
        return TryAnimateScale(node, new Vector2(1.04f, 1.04f), duration);
    }

    /// <summary>
    /// Animates a node's scale back to 1.0x (normal scale).
    /// </summary>
    public static bool TryAnimateScaleDown(Control node, double duration = 0.2)
    {
        return TryAnimateScale(node, Vector2.One, duration);
    }

    /// <summary>
    /// Gets the UIAnimation singleton with null checks and graceful fallback.
    /// </summary>
    private static bool TryGetUIAnimation(out Node? uiAnimation)
    {
        uiAnimation = null;

        var tree = Engine.GetMainLoop() as SceneTree;
        var root = tree?.Root;
        if (root == null)
        {
            return false;
        }

        var singleton = root.GetNodeOrNull<Node>(UIAnimationPath);
        if (singleton == null)
        {
            GD.PrintErr($"UIAnimation singleton not found at path: {UIAnimationPath}. Ensure godot_ui_animations addon is enabled.");
            return false;
        }

        uiAnimation = singleton;
        return true;
    }
}
