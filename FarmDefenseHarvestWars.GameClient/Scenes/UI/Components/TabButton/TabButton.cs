using FarmDefenseHarvestWars.GameClient.Core.Utils;
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

	[Export] public Control ContentRootNode = null!;
	[Export] public NinePatchRect BackgroundNormalNode = null!;
	[Export] public NinePatchRect BackgroundActiveNode = null!;
	[Export] public TextureRect IconNode = null!;

	[ExportGroup("Textures")]
	[Export] public Texture2D IconTexture = null!;

	[ExportGroup("Identity")]
	[Export] public string TabKey = "";

	[ExportGroup("Settings")]
	[Export] public float LiftAmount = -2.0f;
	[Export] public float ActiveLiftAmount = -4.0f;

	private const float AnimationDuration = 0.2f;

	private Vector2 _contentOriginalPos;
	private bool _isHovered = false;
	private TabButtonState _currentState = TabButtonState.Inactive;

	public override void _Ready()
	{
		this.EnsureNotNull(ContentRootNode, nameof(ContentRootNode));
		this.EnsureNotNull(BackgroundNormalNode, nameof(BackgroundNormalNode));
		this.EnsureNotNull(BackgroundActiveNode, nameof(BackgroundActiveNode));
		this.EnsureNotNull(IconNode, nameof(IconNode));

		_contentOriginalPos = ContentRootNode.Position;
		IconNode.PivotOffset = IconNode.Size / 2;

		// Apply custom icon if provided
		IconNode.Texture = IconTexture;

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
		IconNode.Texture = IconTexture;
	}

	public override void _ExitTree()
	{
		// Clean up event subscriptions to prevent memory leaks
		Toggled -= OnToggled;
		MouseEntered -= OnMouseEntered;
		MouseExited -= OnMouseExited;
	}

	private void OnToggled(bool _)
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
		Tween activeTween = ApplyStateVisuals(_currentState);

		activeTween.Finished += OnAnimationFinished;
	}

	private TabButtonState DetermineCurrentState()
	{
		if (ButtonPressed)
			return TabButtonState.Active;

		if (_isHovered)
			return TabButtonState.Hovered;

		return TabButtonState.Inactive;
	}

	private Tween ApplyStateVisuals(TabButtonState state)
	{
		return state switch
		{
			TabButtonState.Active => ApplyActiveState(),
			TabButtonState.Hovered => ApplyHoveredState(),
			_ => ApplyInactiveState(),
		};
	}

	private Tween ApplyActiveState()
	{
		SetActiveBackgroundVisible(true);
		var tween = MoveToHorizontalOffset(ActiveLiftAmount, AnimationDuration);

		// Ensure scale is normal for active state
		UIAnimations.TryAnimateScaleDown(ContentRootNode, AnimationDuration);

		IconNode.ResetShake();
		return tween;
	}

	private Tween ApplyHoveredState()
	{
		SetActiveBackgroundVisible(false);
		var tween = MoveToHorizontalOffset(LiftAmount, AnimationDuration);

		// Apply hover scale effect (1.04x)
		UIAnimations.TryAnimateScaleUp(ContentRootNode, AnimationDuration);

		IconNode.Shake();
		return tween;
	}

	private Tween ApplyInactiveState()
	{
		// Ensure scale is normal for inactive state
		UIAnimations.TryAnimateScaleDown(ContentRootNode, AnimationDuration);

		SetActiveBackgroundVisible(false);
		var tween = MoveToHorizontalOffset(0, AnimationDuration);

		IconNode.ResetShake();
		return tween;
	}

	private void SetActiveBackgroundVisible(bool isActive)
	{
		BackgroundActiveNode.Visible = isActive;
		BackgroundNormalNode.Visible = !isActive;
	}

	private void OnAnimationFinished()
	{
		// Emit signal to parent when animation completes, including which state animation finished
		EmitSignal(SignalName.AnimationFinished, this, Variant.From(_currentState));
	}

	private Tween MoveToHorizontalOffset(float offsetX, float duration)
	{
		Vector2 targetPos = _contentOriginalPos + new Vector2(offsetX, 0);
		return ContentRootNode.AnimatePosition(targetPos, duration);
	}
}