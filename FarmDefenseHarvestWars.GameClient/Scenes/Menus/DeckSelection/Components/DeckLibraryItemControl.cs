using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;
using FarmDefenseHarvestWars.Shared.Enums;

public partial class DeckLibraryItemControl : PanelContainer
{
	[Export] private TextureRect _icon = null!;
	[Export] private Label _statusLabel = null!;
	[Export] private Label _levelLabel = null!;
	[Export] private Button _infoButton = null!;
	[Export] private PackedScene _dragPreviewScene = null!;
	[Export] private PackedScene _tooltipScene = null!;
	[Export] private PackedScene _upgradePopupScene = null!;

	private UnitData? _unitData;
	private int _unitTypeValue;
	private bool _canDrag;
	private bool _isUnlocked;
	private bool _isUnlocking;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Pass;

		this.EnsureNotNull(_icon, nameof(_icon));
		this.EnsureNotNull(_statusLabel, nameof(_statusLabel));
		// We don't fail-fast on level and info yet to allow legacy scenes to work
		// this.EnsureNotNull(_levelLabel, nameof(_levelLabel)); 

		this.EnsureNotNull(_dragPreviewScene, nameof(_dragPreviewScene));
		this.EnsureNotNull(_tooltipScene, nameof(_tooltipScene));

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;

		_statusLabel.Hide();

		if (_infoButton != null)
		{
			_infoButton.Pressed += OnInfoButtonPressed;
			_infoButton.MouseExited += OnMouseExited;
			_infoButton.Hide();
		}
	}

	public void Setup(UnitData unitData, bool alreadyInDeck, bool isUnlocked, bool isUnlocking, bool isDeckSaving)
	{
		_unitData = unitData;
		_unitTypeValue = (int)unitData.Type;
		_isUnlocked = isUnlocked;
		_isUnlocking = isUnlocking;
		_canDrag = !alreadyInDeck && isUnlocked && !isUnlocking && !isDeckSaving;

		_icon.Texture = unitData.Icon;

		// Status display
		if (!isUnlocked)
		{
			_statusLabel.Text = isUnlocking ? "..." : "L";
			_statusLabel.Show();
			_icon.Modulate = new Color(0.2f, 0.2f, 0.2f, 0.8f);
			SelfModulate = new Color(0.8f, 0.8f, 0.8f, 0.6f);
		}
		else if (alreadyInDeck)
		{
			_statusLabel.Text = "D";
			_statusLabel.Show();
			_icon.Modulate = new Color(0.6f, 0.6f, 0.6f, 0.7f);
			SelfModulate = new Color(0.9f, 0.9f, 0.9f, 0.8f);
		}
		else
		{
			_statusLabel.Hide();
			_icon.Modulate = Colors.White;
			SelfModulate = Colors.White;
		}

		TooltipText = isUnlocked
			? unitData.Name
			: isUnlocking
				? $"Unlock in progress for {unitData.Name}"
				: $"Click to unlock {unitData.Name} for {unitData.UnlockCost} gold";

		MouseDefaultCursorShape = _canDrag
			? CursorShape.Drag
			: isDeckSaving
				? CursorShape.Busy
			: isUnlocking
				? CursorShape.Busy
				: isUnlocked
				? CursorShape.Forbidden
				: CursorShape.PointingHand;

		// Level display (at the very end to ensure it stays visible)
		if (_levelLabel != null)
		{
			var unlock = GameState.Instance?.GetUnitUnlock(unitData.Role, unitData.Type);
			_levelLabel.Text = unlock != null ? $"Lvl {unlock.Level}" : "Lvl 1";
			_levelLabel.Show();
			_levelLabel.ZIndex = 10; // Ensure it's on top
			GD.Print($"[DeckLibraryItem] FINAL Setup level for {unitData.Name}: {_levelLabel.Text} (Visible: {_levelLabel.Visible}, Unlocked: {isUnlocked}, Pos: {_levelLabel.Position})");
		}
	}

	public override void _ExitTree()
	{
		MouseEntered -= OnMouseEntered;
		MouseExited -= OnMouseExited;
		if (_infoButton != null)
		{
			_infoButton.MouseExited -= OnMouseExited;
		}
	}

	public void OnMouseEntered()
	{
		if (_isUnlocked)
		{
			if (!_infoButton.Visible)
			{
				_infoButton.Show();
				UIAnimations.AnimatePop(_infoButton);
			}
		}

		if (!_isUnlocked && !_isUnlocking)
		{
			UIAnimations.TryAnimateScale(this, new Vector2(1.05f, 1.05f), 0.15);
			return;
		}

		UIAnimations.TryAnimateScaleUp(this, 0.15);
	}

	public void OnMouseExited()
	{
		// Don't hide if the mouse is actually still inside the control OR over the info button (which might protrude)
		var mousePos = GetGlobalMousePosition();
		if (GetGlobalRect().HasPoint(mousePos) || (_infoButton != null && _infoButton.Visible && _infoButton.GetGlobalRect().HasPoint(mousePos)))
		{
			return;
		}

		if (_infoButton != null && _infoButton.Visible)
		{
			_infoButton.Hide();
			UIAnimations.AnimateShrink(_infoButton);
		}

		UIAnimations.TryAnimateScaleDown(this, 0.15);
	}


	private void OnInfoButtonPressed()
	{
		if (_unitData == null || _upgradePopupScene == null) return;

		var unlock = GameState.Instance?.GetUnitUnlock(_unitData.Role, _unitData.Type);
		if (unlock == null) return;

		var popup = _upgradePopupScene.Instantiate<UpgradePopup>();
		GetTree().Root.AddChild(popup);
		popup.Setup(_unitData, unlock);
	}


	public override void _GuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton)
		{
			return;
		}

		if (!mouseButton.Pressed || mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (_isUnlocked || _isUnlocking)
		{
			return;
		}

		AcceptEvent();
	}

	private Control CreateDragPreview(Texture2D texture)
	{
		var preview = _dragPreviewScene.Instantiate<DeckDragPreviewControl>();
		if (preview == null)
		{
			return new Control();
		}

		preview.Setup(texture);
		preview.Modulate = new Color(1f, 1f, 1f, 0.85f);
		return preview;
	}

	public override Control _MakeCustomTooltip(string forText)
	{
		if (_unitData == null || _tooltipScene == null) return null!;

		var tooltip = _tooltipScene.Instantiate<UnitTooltip>();
		var unlock = GameState.Instance?.GetUnitUnlock(_unitData.Role, _unitData.Type);
		tooltip.Setup(_unitData, unlock);
		return tooltip;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (!_canDrag)
		{
			return default;
		}

		var payload = new Godot.Collections.Dictionary
		{
			["unitType"] = _unitTypeValue,
			["fromSlot"] = -1
		};

		SetDragPreview(CreateDragPreview(_icon.Texture));

		return payload;
	}
}
