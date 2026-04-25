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
    private const float DefaultOffset = 8.0f;
    private const float DefaultSpeed = 0.3f;

    #region Addon Wrapper Methods

    public static Signal? AnimatePop(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_pop", node, speed);
    }

    public static Signal? AnimateShrink(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_shrink", node, speed);
    }

    public static Signal? AnimateSlideFromLeft(Control node, float offset = DefaultOffset, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_slide_from_left", node, offset, speed);
    }

    public static Signal? AnimateSlideToLeft(Control node, float offset = DefaultOffset, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_slide_to_left", node, offset, speed);
    }

    public static Signal? AnimateSlideFromRight(Control node, float offset = DefaultOffset, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_slide_from_right", node, offset, speed);
    }

    public static Signal? AnimateSlideToRight(Control node, float offset = DefaultOffset, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_slide_to_right", node, offset, speed);
    }

    public static Signal? AnimateFromLeftToCenter(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_from_left_to_center", node, speed);
    }

    public static Signal? AnimateFromCenterToLeft(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_from_center_to_left", node, speed);
    }

    public static Signal? AnimateFromRightToCenter(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_from_right_to_center", node, speed);
    }

    public static Signal? AnimateFromCenterToRight(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_from_center_to_right", node, speed);
    }

    public static Signal? AnimateSlideFromTop(Control node, float offset = DefaultOffset, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_slide_from_top", node, offset, speed);
    }

    public static Signal? AnimateSlideToTop(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_slide_to_top", node, speed);
    }

    public static Signal? AnimateShrinkX(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_shrink_x", node, speed);
    }

    public static Signal? AnimateShrinkY(Control node, float speed = DefaultSpeed)
    {
        if (node == null || !TryGetUIAnimation(out var uiAnimation)) return null;
        return (Signal)uiAnimation!.Call("animate_shrink_y", node, speed);
    }

    #endregion

    #region Internal Scale Helpers

    /// <summary>
    /// Animates a node's scale to a specific target value using a local tween.
    /// </summary>
    public static bool TryAnimateScale(Control node, Vector2 targetScale, double duration)
    {
        if (node == null) return false;

        try
        {
            node.PivotOffset = new Vector2(node.Size.X / 2, node.Size.Y / 2);
            Tween tween = node.CreateTween();
            tween.SetTrans(Tween.TransitionType.Back);
            tween.SetEase(Tween.EaseType.Out);
            tween.TweenProperty(node, "scale", targetScale, duration);
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"UIAnimations local animate failed: {ex.Message}");
            return false;
        }
    }

    public static bool TryAnimateScaleUp(Control node, double duration = 0.2)
        => TryAnimateScale(node, new Vector2(1.04f, 1.04f), duration);

    public static bool TryAnimateScaleDown(Control node, double duration = 0.2)
        => TryAnimateScale(node, Vector2.One, duration);

    #endregion

    /// <summary>
    /// Gets the UIAnimation singleton with null checks and graceful fallback.
    /// </summary>
    private static bool TryGetUIAnimation(out Node? uiAnimation)
    {
        uiAnimation = null;
        var tree = Engine.GetMainLoop() as SceneTree;
        var root = tree?.Root;
        if (root == null) return false;

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
