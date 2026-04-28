using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;
using FarmDefenseHarvestWars.Shared.Models.Game;

public partial class DeckSlotControl : PanelContainer
{
    [Signal]
    public delegate void SlotDropRequestedEventHandler(int targetIndex, int unitTypeValue, int fromSlotIndex);

    [Export] public int SlotIndex = 0;

    [Export] private TextureRect _icon = null!;
    [Export] private Label _levelLabel = null!;
    [Export] private Control _emptyIcon = null!;
    [Export] private PackedScene _dragPreviewScene = null!;
    [Export] private PackedScene _tooltipScene = null!;
    [Export] private PackedScene _upgradePopupScene = null!;
    [Export] private Button _infoButton = null!;
    private UnitData? _unitData;
    private UnitType? _unitType;
    private PlayerRole _contextRole;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_emptyIcon, nameof(_emptyIcon));
        this.EnsureNotNull(_dragPreviewScene, nameof(_dragPreviewScene));

        if (_levelLabel != null) _levelLabel.Hide();

        if (_infoButton != null)
        {
            _infoButton.Pressed += OnInfoButtonPressed;
            _infoButton.Hide();
        }

        GameState.Instance.UnitUpgraded += OnUnitUpgraded;

        if (_tooltipScene == null)
        {
            GD.PrintErr($"[DeckSlotControl] Tooltip scene is null on {Name}. Attempting to load fallback.");
            _tooltipScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/Components/UnitTooltip.tscn");
        }
        this.EnsureNotNull(_tooltipScene, nameof(_tooltipScene));

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        UpdateVisual();
    }

    public override void _ExitTree()
    {
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
        if (GameState.Instance != null)
        {
            GameState.Instance.UnitUpgraded -= OnUnitUpgraded;
        }
    }

    public void OnMouseEntered()
    {
        if (_unitData != null && _infoButton != null)
        {
            _infoButton.Show();
            UIAnimations.AnimatePop(_infoButton);
        }
        UIAnimations.TryAnimateScale(this, new Vector2(1.1f, 1.1f), 0.15);
    }

    public void OnMouseExited()
    {
        var mousePos = GetGlobalMousePosition();
        if (GetGlobalRect().HasPoint(mousePos) || (_infoButton != null && _infoButton.Visible && _infoButton.GetGlobalRect().HasPoint(mousePos)))
        {
            return;
        }

        if (_infoButton != null)
        {
            _infoButton.Hide();
            UIAnimations.AnimateShrink(_infoButton);
        }
        UIAnimations.TryAnimateScale(this, Vector2.One, 0.15);
    }

    public void SetUnit(UnitData unitData, PlayerRole contextRole = PlayerRole.Any)
    {
        _unitData = unitData;
        _unitType = unitData.Type;
        _contextRole = contextRole;
        _icon.Texture = unitData.Icon;
        TooltipText = unitData.Name;

        if (_levelLabel != null)
        {
            var effectiveRole = (unitData.Role == PlayerRole.Any) ? _contextRole : unitData.Role;
            var unlock = GameState.Instance?.GetUnitUnlock(effectiveRole, unitData.Type);
            _levelLabel.Text = unlock != null ? $"Lvl {unlock.Level}" : "Lvl 1";
            _levelLabel.Show();
            _levelLabel.ZIndex = 10;
            GD.Print($"[DeckSlot] SetUnit for {unitData.Name}: {_levelLabel.Text} (Context: {_contextRole})");
        }
        
        // Juiciness: Pop effect when setting a unit
        UIAnimations.AnimatePop(this);
        
        UpdateVisual();
    }

    public void ClearUnit()
    {
        _unitData = null;
        _unitType = null;
        _icon.Texture = null;
        if (_levelLabel != null) _levelLabel.Hide();
        if (_infoButton != null) _infoButton.Hide();
        UpdateVisual();
    }

    private void OnInfoButtonPressed()
    {
        if (_unitData == null || _upgradePopupScene == null) return;

        var effectiveRole = (_unitData.Role == PlayerRole.Any) ? _contextRole : _unitData.Role;
        var unlock = GameState.Instance?.GetUnitUnlock(effectiveRole, _unitData.Type);
        
        // If unit is unlocked but no unlock data in profile, create a dummy Lvl 1
        if (unlock == null)
        {
            unlock = new UnitUnlockDto
            {
                UnitType = _unitData.Type,
                Level = 1,
                Fragments = 0
            };
        }

        var popup = _upgradePopupScene.Instantiate<UpgradePopup>();
        GetTree().Root.AddChild(popup);
        popup.Setup(_unitData, unlock, _contextRole);
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

    public override Control _MakeCustomTooltip(string forText)
    {
        if (_unitData == null || _tooltipScene == null) return null!;

        var tooltip = _tooltipScene.Instantiate<UnitTooltip>();
        var effectiveRole = (_unitData.Role == PlayerRole.Any) ? _contextRole : _unitData.Role;
        var unlock = GameState.Instance?.GetUnitUnlock(effectiveRole, _unitData.Type);
        tooltip.Setup(_unitData, unlock);
        return tooltip;
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

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (!_unitType.HasValue)
        {
            return default;
        }

        var payload = new Godot.Collections.Dictionary
        {
            ["unitType"] = (int)_unitType.Value,
            ["fromSlot"] = SlotIndex
        };

        SetDragPreview(CreateDragPreview(_icon.Texture));

        return payload;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
        {
            return false;
        }

        var dict = data.AsGodotDictionary();
        return dict.ContainsKey("unitType");
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
        {
            return;
        }

        var dict = data.AsGodotDictionary();
        if (!dict.ContainsKey("unitType"))
        {
            return;
        }

        int unitTypeValue = ReadInt(dict, "unitType", 0);
        int fromSlotIndex = ReadInt(dict, "fromSlot", -1);

        EmitSignal(SignalName.SlotDropRequested, SlotIndex, unitTypeValue, fromSlotIndex);
    }

    private void UpdateVisual()
    {
        bool hasUnit = _unitType.HasValue;
        _icon.Visible = hasUnit;
        _emptyIcon.Visible = !hasUnit;

        SelfModulate = hasUnit
            ? Colors.White
            : new Color(1f, 1f, 1f, 0.4f);
    }

    private static int ReadInt(Godot.Collections.Dictionary dict, string key, int fallback)
    {
        if (!dict.ContainsKey(key))
        {
            return fallback;
        }

        var value = dict[key];
        if (value.VariantType == Variant.Type.Int)
        {
            return (int)(long)value;
        }

        return fallback;
    }
}
