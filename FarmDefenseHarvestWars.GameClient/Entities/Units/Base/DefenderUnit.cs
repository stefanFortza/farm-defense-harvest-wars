using Godot;

public abstract partial class DefenderUnit : BaseUnit
{
    // Defender units are static, so no movement logic here.
    // They might have an action timer for shooting or producing resources.

    [Export] public float ActionInterval { get; set; } = 1.0f;
    protected Timer _actionTimer;

    public override void _Ready()
    {
        base._Ready();

        // Get Timer from Scene
        _actionTimer = GetNode<Timer>("ActionTimer");
        _actionTimer.WaitTime = ActionInterval;
        _actionTimer.OneShot = false;
        _actionTimer.Timeout += OnActionTimerTimeout;
        {
            // Override this in subclasses (e.g., Cow blocks, Chicken shoots)
        }
    }
