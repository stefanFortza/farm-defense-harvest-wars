using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

public partial class DeckLibraryItemControl : PanelContainer
{
	[Signal] public delegate void UnlockRequestedEventHandler(int unitType);

	[Export] private TextureRect _icon = null!;
	[Export] private Label _statusLabel = null!;
	[Export] private Label _levelLabel = null!;
	[Export] private Label _unlockPriceLabel = null!;
	[Export] private Button _infoButton = null!;
	[Export] private PackedScene _dragPreviewScene = null!;
	[Export] private PackedScene _tooltipScene = null!;
	[Export] private PackedScene _upgradePopupScene = null!;

	private UnitData? _unitData;
	private int _unitTypeValue;
	private PlayerRole _contextRole;
	private bool _canDrag;
	private bool _isUnlocked;
	private bool _isUnlocking;

	public override void _Ready()
	{
		this.EnsureNotNull(_icon, nameof(_icon));
		this.EnsureNotNull(_statusLabel, nameof(_statusLabel));
		this.EnsureNotNull(_unlockPriceLabel, nameof(_unlockPriceLabel));
		// We don't fail-fast on level and info yet to allow legacy scenes to work
		// this.EnsureNotNull(_levelLabel, nameof(_levelLabel)); 
		this.EnsureNotNull(_infoButton, nameof(_infoButton));

		this.EnsureNotNull(_dragPreviewScene, nameof(_dragPreviewScene));
		this.EnsureNotNull(_tooltipScene, nameof(_tooltipScene));

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;

		if (_isUnlocked)
		{
			_statusLabel.Hide();
			_unlockPriceLabel.Hide();
		}


		_infoButton.Pressed += OnInfoButtonPressed;
		_infoButton.MouseExited += OnMouseExited;
		_infoButton.Hide();

		GameState.Instance.UnitUpgraded += OnUnitUpgraded;
	}

	public void Setup(UnitData unitData, bool alreadyInDeck, bool isUnlocked, bool isUnlocking, bool isDeckSaving, PlayerRole contextRole = PlayerRole.Any)
	{
		_unitData = unitData;
		_unitTypeValue = (int)unitData.Type;
		_contextRole = contextRole;
		_isUnlocked = isUnlocked;
		_isUnlocking = isUnlocking;
		_canDrag = !alreadyInDeck && isUnlocked && !isUnlocking && !isDeckSaving;

		_icon.Texture = unitData.Icon;

		// Status display
		if (!isUnlocked)
		{
			_statusLabel.Text = isUnlocking ? "..." : "Locked";
			_statusLabel.Show();

			_unlockPriceLabel.Text = $"Price: {unitData.UnlockCost}";
			_unlockPriceLabel.Show();

			_icon.Modulate = new Color(0.2f, 0.2f, 0.2f, 0.8f);
			SelfModulate = new Color(0.8f, 0.8f, 0.8f, 0.6f);
			GD.Print($"Unit {unitData.Name} is locked. Unlock cost: {unitData.UnlockCost}"); // Debug log
		}
		else if (alreadyInDeck)
		{
			_statusLabel.Text = "D";
			_statusLabel.Show();
			_unlockPriceLabel.Hide();
			_icon.Modulate = new Color(0.6f, 0.6f, 0.6f, 0.7f);
			SelfModulate = new Color(0.9f, 0.9f, 0.9f, 0.8f);
		}
		else
		{
			_statusLabel.Hide();
			_unlockPriceLabel.Hide();
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
			if (!isUnlocked)
			{
				_levelLabel.Hide();
			}
			else
			{
				var effectiveRole = (unitData.Role == PlayerRole.Any) ? _contextRole : unitData.Role;
				var unlock = GameState.Instance?.GetUnitUnlock(effectiveRole, unitData.Type);
				_levelLabel.Text = unlock != null ? $"Lvl {unlock.Level}" : "Lvl 1";
				_levelLabel.Show();
				_levelLabel.ZIndex = 10; // Ensure it's on top
			}
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
		if (GameState.Instance != null)
		{
			GameState.Instance.UnitUpgraded -= OnUnitUpgraded;
		}
	}

	public void OnMouseEntered()
	{
		if (_isUnlocked)
		{
			if (_infoButton != null && !_infoButton.Visible)
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

	private void OnUnitUpgraded(int unitType, int newLevel)
	{
		if (_unitData != null && (int)_unitData.Type == unitType)
		{
			if (_levelLabel != null)
			{
				_levelLabel.Text = $"Lvl {newLevel}";
				UIAnimations.AnimatePop(_levelLabel);
			}
		}
	}


	private void OnInfoButtonPressed()
	{
		if (_unitData == null || _upgradePopupScene == null) return;

		var effectiveRole = (_unitData.Role == PlayerRole.Any) ? _contextRole : _unitData.Role;
		var unlock = GameState.Instance?.GetUnitUnlock(effectiveRole, _unitData.Type);

		// If unit is unlocked (e.g. default) but no unlock data in profile, create a dummy Lvl 1
		if (unlock == null && _isUnlocked)
		{
			unlock = new UnitUnlockDto
			{
				UnitType = _unitData.Type,
				Level = 1,
				Fragments = 0
			};
		}

		if (unlock == null) return;

		var popup = _upgradePopupScene.Instantiate<UpgradePopup>();
		GetTree().Root.AddChild(popup);
		popup.Setup(_unitData, unlock, _contextRole);
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

		if (_unitData != null)
		{
			EmitSignal(SignalName.UnlockRequested, (int)_unitData.Type);
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
