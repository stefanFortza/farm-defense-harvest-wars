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
	[Export] public Shader? ButtonShader;

	private const float AnimationDuration = 0.2f;

	private Vector2 _contentOriginalPos;
	private bool _isHovered = false;
	private TabButtonState _currentState = TabButtonState.Inactive;
	private ShaderMaterial? _shaderMaterial;

	public override void _Ready()
	{
		this.EnsureNotNull(ContentRootNode, nameof(ContentRootNode));
		this.EnsureNotNull(BackgroundNormalNode, nameof(BackgroundNormalNode));
		this.EnsureNotNull(BackgroundActiveNode, nameof(BackgroundActiveNode));
		this.EnsureNotNull(IconNode, nameof(IconNode));

		_contentOriginalPos = ContentRootNode.Position;
		IconNode.PivotOffset = IconNode.Size / 2;

		// Setup Shader (Only for the Icon)
		SetupShader();

		// Apply custom icon if provided
		IconNode.Texture = IconTexture;

		// Connect event handlers
		Toggled += OnToggled;
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;

		// Initialize visual state
		UpdateVisualState();
	}

	private void SetupShader()
	{
		if (ButtonShader == null) return;

		_shaderMaterial = new ShaderMaterial { Shader = ButtonShader };
		
		// Desynchronize shine
		float randomOffset = GD.Randf() * 10.0f;
		_shaderMaterial.SetShaderParameter("shine_time_offset", randomOffset);
		_shaderMaterial.SetShaderParameter("shine_size", 0.05f);
		
		// Apply ONLY to the Icon node
		IconNode.Material = _shaderMaterial;
	}

	private void AnimateHoverShader(float target)
	{
		if (_shaderMaterial != null)
		{
			var tween = GetTree().CreateTween();
			tween.TweenMethod(Callable.From<float>((val) => _shaderMaterial.SetShaderParameter("hover_intensity", val)), 
				(float)_shaderMaterial.GetShaderParameter("hover_intensity"), target, AnimationDuration);
		}
	}

	public void SetIconTexture(Texture2D texture)
	{
		IconTexture = texture;
		IconNode.Texture = IconTexture;
	}

	public override void _ExitTree()
	{
		Toggled -= OnToggled;
		MouseEntered -= OnMouseEntered;
		MouseExited -= OnMouseExited;
	}

	private void OnToggled(bool _)
	{
		UpdateVisualState();
		AudioController.Instance?.PlaySfx("res://Assets/Audio/ui/switch1.ogg");
	}

	private void OnMouseEntered()
	{
		_isHovered = true;
		UpdateVisualState();
		AnimateHoverShader(1.0f);
		AudioController.Instance?.PlaySfx("res://Assets/Audio/ui/rollover1.ogg");
	}

	private void OnMouseExited()
	{
		_isHovered = false;
		UpdateVisualState();
		AnimateHoverShader(0.0f);
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
		UIAnimations.TryAnimateScaleDown(ContentRootNode, AnimationDuration);
		IconNode.ResetShake();
		return tween;
	}

	private Tween ApplyHoveredState()
	{
		SetActiveBackgroundVisible(false);
		var tween = MoveToHorizontalOffset(LiftAmount, AnimationDuration);
		UIAnimations.TryAnimateScaleUp(ContentRootNode, AnimationDuration);
		IconNode.Shake();
		return tween;
	}

	private Tween ApplyInactiveState()
	{
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
		EmitSignal(SignalName.AnimationFinished, this, Variant.From(_currentState));
	}

	private Tween MoveToHorizontalOffset(float offsetX, float duration)
	{
		Vector2 targetPos = _contentOriginalPos + new Vector2(offsetX, 0);
		return ContentRootNode.AnimatePosition(targetPos, duration);
	}
}
