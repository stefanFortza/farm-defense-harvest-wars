using Godot;

public partial class TabButton : TextureButton
{
	public enum TabButtonState
	{
		Inactive,
		Hovered,
		Active
	}

	// Custom Signal emitted when animation finishes
	[Signal]
	public delegate void AnimationFinishedEventHandler(TabButton buttonChanged, TabButtonState finalState);

	[Export] public NinePatchRect BackgroundNode = null!;
	[Export] public TextureRect IconNode = null!;

	[ExportGroup("Textures")]
	[Export] public Texture2D NormalTexture = null!;
	[Export] public Texture2D ActiveTexture = null!;
	[Export] public Texture2D IconTexture = null!;

	[ExportGroup("Identity")]
	[Export] public string TabKey = "";

	[ExportGroup("Settings")]
	[Export] public float LiftAmount = -2.0f;
	[Export] public float ActiveLiftAmount = -4.0f; // Tab-ul activ stă și mai sus?

	private Vector2 _bgOriginalPos;
	private bool _isHovered = false;
	private TabButtonState _currentState = TabButtonState.Inactive;

	public override void _Ready()
	{
		BackgroundNode ??= GetNodeOrNull<NinePatchRect>("NinePatchRect");
		IconNode ??= GetNodeOrNull<TextureRect>("NinePatchRect/Icon");

		_bgOriginalPos = BackgroundNode.Position;
		IconNode.PivotOffset = IconNode.Size / 2;

		// Apply custom icon if provided
		if (IconNode != null && IconTexture != null)
		{
			IconNode.Texture = IconTexture;
		}

		// Connect event handlers - using separate methods for better debuggability and testability
		Toggled += OnToggled;
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;

		// Initialize visual state
		UpdateVisualState();
	}

	public void SetIconTexture(Texture2D texture)
	{
		IconTexture = texture;
		if (IconNode != null)
		{
			IconNode.Texture = IconTexture;
		}
	}

	public override void _ExitTree()
	{
		// Clean up event subscriptions to prevent memory leaks
		Toggled -= OnToggled;
		MouseEntered -= OnMouseEntered;
		MouseExited -= OnMouseExited;
	}

	private void OnToggled(bool pressed)
	{
		UpdateVisualState();
	}

	private void OnMouseEntered()
	{
		_isHovered = true;
		UpdateVisualState();
	}

	private void OnMouseExited()
	{
		_isHovered = false;
		UpdateVisualState();
	}

	private void UpdateVisualState()
	{
		_currentState = DetermineCurrentState();
		Tween? activeTween = ApplyStateVisuals(_currentState);

		// If we have an active tween, connect to its Finished signal
		if (activeTween != null)
		{
			activeTween.Finished += OnAnimationFinished;
		}
	}

	private TabButtonState DetermineCurrentState()
	{
		if (ButtonPressed)
			return TabButtonState.Active;

		if (_isHovered)
			return TabButtonState.Hovered;

		return TabButtonState.Inactive;
	}

	private Tween? ApplyStateVisuals(TabButtonState state)
	{
		switch (state)
		{
			case TabButtonState.Active:
				return ApplyActiveState();

			case TabButtonState.Hovered:
				return ApplyHoveredState();

			case TabButtonState.Inactive:
			default:
				return ApplyInactiveState();
		}
	}

	private Tween ApplyActiveState()
	{
		var tween = MoveToOffset(ActiveLiftAmount, 0.2f);

		if (BackgroundNode != null && ActiveTexture != null)
		{
			BackgroundNode.Texture = ActiveTexture;
			BackgroundNode.Modulate = Colors.White;
		}

		IconNode?.ResetShake();
		return tween;
	}

	private Tween ApplyHoveredState()
	{
		var tween = MoveToOffset(LiftAmount, 0.2f);

		if (BackgroundNode != null && NormalTexture != null)
		{
			BackgroundNode.Texture = NormalTexture;
		}

		IconNode?.Shake();
		return tween;
	}

	private Tween ApplyInactiveState()
	{
		var tween = MoveToOffset(0, 0.2f);

		if (BackgroundNode != null && NormalTexture != null)
		{
			BackgroundNode.Texture = NormalTexture;
			BackgroundNode.Modulate = Colors.White;
		}

		IconNode?.ResetShake();
		return tween;
	}

	private void OnAnimationFinished()
	{
		// Emit signal to parent when animation completes, including which state animation finished
		EmitSignal(SignalName.AnimationFinished, this, Variant.From(_currentState));
	}

	private Tween MoveToOffset(float offsetY, float duration)
	{
		if (BackgroundNode != null)
		{
			Vector2 targetPos = _bgOriginalPos + new Vector2(0, offsetY);
			return BackgroundNode.AnimatePosition(targetPos, duration);
		}
		return null;
	}
}