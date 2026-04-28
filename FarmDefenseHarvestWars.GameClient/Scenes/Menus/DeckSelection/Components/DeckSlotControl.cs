using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;

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
    private UnitData? _unitData;
    private UnitType? _unitType;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_emptyIcon, nameof(_emptyIcon));
        this.EnsureNotNull(_dragPreviewScene, nameof(_dragPreviewScene));

        if (_levelLabel != null) _levelLabel.Hide();

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
    }

    public void OnMouseEntered()
    {
        UIAnimations.TryAnimateScale(this, new Vector2(1.1f, 1.1f), 0.15);
    }

    public void OnMouseExited()
    {
        UIAnimations.TryAnimateScale(this, Vector2.One, 0.15);
    }

    public void SetUnit(UnitData unitData)
    {
        _unitData = unitData;
        _unitType = unitData.Type;
        _icon.Texture = unitData.Icon;
        TooltipText = unitData.Name;

        if (_levelLabel != null)
        {
            var unlock = GameState.Instance?.GetUnitUnlock(unitData.Role, unitData.Type);
            _levelLabel.Text = unlock != null ? $"Lvl {unlock.Level}" : "Lvl 1";
            _levelLabel.Show();
            _levelLabel.ZIndex = 10;
            GD.Print($"[DeckSlot] SetUnit for {unitData.Name}: {_levelLabel.Text}");
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
        UpdateVisual();
    }

    public override Control _MakeCustomTooltip(string forText)
    {
        if (_unitData == null || _tooltipScene == null) return null!;

        var tooltip = _tooltipScene.Instantiate<UnitTooltip>();
        var unlock = GameState.Instance?.GetUnitUnlock(_unitData.Role, _unitData.Type);
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
