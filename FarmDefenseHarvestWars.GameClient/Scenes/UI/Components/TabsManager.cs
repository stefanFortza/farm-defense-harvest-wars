using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Manages a group of TabButton components. Handles Z-Index synchronization
/// and other visual effects that should happen when animations complete.
/// 
/// Attach this script to the HBoxContainer that contains your TabButton children.
/// </summary>
public partial class TabsManager : HBoxContainer
{
    // Optional: assign explicitly via Inspector; falls back to ../ContentOverlay/LeftZone and RightZone
    [Export] public NodePath LeftZonePath = null!;
    [Export] public NodePath RightZonePath = null!;

    private Control? _leftZone;
    private Control? _rightZone;

    // Pages discovered in zones by TabKey (node names should match TabKey)
    private readonly Dictionary<string, Control?> _leftPages = new();
    private readonly Dictionary<string, Control?> _rightPages = new();
    public override void _Ready()
    {
        // Resolve zones (explicit NodePaths preferred; fallback to relative defaults)
        _leftZone = LeftZonePath != null ? GetNodeOrNull<Control>(LeftZonePath) : GetNodeOrNull<Control>("../ContentOverlay/LeftZone");
        _rightZone = RightZonePath != null ? GetNodeOrNull<Control>(RightZonePath) : GetNodeOrNull<Control>("../ContentOverlay/RightZone");

        // Iterate through all children and connect to TabButton signals
        foreach (var child in GetChildren())
        {
            if (child is TabButton tabBtn)
            {
                // Connect to custom animation finished signal
                tabBtn.AnimationFinished += OnTabAnimationFinished;

                // Also connect to Toggled for immediate Z-Index response
                tabBtn.Toggled += (pressed) => OnTabToggled(tabBtn, pressed);
            }
        }

        // Discover pre-placed pages by key (node name == TabKey) and hide all
        BuildPagesFromZones();

        // Initialize visibility based on the currently active tab (if any)
        var activeTab = GetChildren().OfType<TabButton>().FirstOrDefault(t => t.ButtonPressed);
        if (activeTab != null)
        {
            ShowTabByKey(activeTab.TabKey);
        }
        else
        {
            // If none pressed, hide all
            HideAllPages();
        }
    }

    /// <summary>
    /// Called INSTANTLY when a tab is toggled (before animation starts).
    /// Sets up immediate visual changes like Z-Index to prevent overlap issues.
    /// </summary>
    private void OnTabToggled(TabButton btn, bool pressed)
    {
        if (pressed)
        {
            ShowTabByKey(btn.TabKey);
        }
        else
        {
            // Inactive tabs go back to normal depth
            btn.ZIndex = 0;
        }
    }

    private void OnTabAnimationFinished(TabButton btn, TabButton.TabButtonState finalState)
    {

        switch (finalState)
        {
            case TabButton.TabButtonState.Active:
                GD.Print($"Tab animation finished: {btn.Name} → ACTIVE");
                btn.ZIndex = 10;
                // Optional: Apply scale or other effects specific to active state
                // btn.BackgroundNode.Scale = new Vector2(1.05f, 1.05f);
                // Ensure content reflects the active tab after animation
                ShowTabByKey(btn.TabKey);
                break;

            case TabButton.TabButtonState.Hovered:
                GD.Print($"Tab animation finished: {btn.Name} → HOVERED");
                // Hover animations don't typically need special handling after completion
                break;

            case TabButton.TabButtonState.Inactive:
                GD.Print($"Tab animation finished: {btn.Name} → INACTIVE");
                btn.ZIndex = 0;
                // Optional: Reset any effects
                // btn.BackgroundNode.Scale = Vector2.One;
                break;
        }
    }

    private void BuildPagesFromZones()
    {
        // Left pages
        if (_leftZone != null)
        {
            foreach (var child in _leftZone.GetChildren())
            {
                if (child is Control n)
                {
                    n.Visible = false;
                    _leftPages[n.Name] = n;
                }
            }
        }

        // Right pages
        if (_rightZone != null)
        {
            foreach (var child in _rightZone.GetChildren())
            {
                if (child is Control n)
                {
                    n.Visible = false;
                    _rightPages[n.Name] = n;
                }
            }
        }
    }

    private void HideAllPages()
    {
        foreach (var n in _leftPages.Values)
        {
            n?.Visible = false;
        }
        foreach (var n in _rightPages.Values)
        {
            n?.Visible = false;
        }
    }

    private void ShowTabByKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            HideAllPages();
            return;
        }

        HideAllPages();

        if (_leftPages.TryGetValue(key, out var left) && left != null)
        {
            left.Visible = true;
        }
        if (_rightPages.TryGetValue(key, out var right) && right != null)
        {
            right.Visible = true;
        }
    }
}
