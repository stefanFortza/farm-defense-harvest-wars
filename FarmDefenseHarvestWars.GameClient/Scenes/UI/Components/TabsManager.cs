using Godot;

/// <summary>
/// Manages a group of TabButton components. Handles Z-Index synchronization
/// and other visual effects that should happen when animations complete.
/// 
/// Attach this script to the HBoxContainer that contains your TabButton children.
/// </summary>
public partial class TabsManager : HBoxContainer
{
    public override void _Ready()
    {
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
    }

    /// <summary>
    /// Called INSTANTLY when a tab is toggled (before animation starts).
    /// Sets up immediate visual changes like Z-Index to prevent overlap issues.
    /// </summary>
    private void OnTabToggled(TabButton btn, bool pressed)
    {
        if (pressed)
        {
            // Active tab should be drawn on top during the lift animation
            // btn.ZIndex = 10;
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
}
