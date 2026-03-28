using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;

public partial class DeckSlotControl : PanelContainer
{
    [Signal]
    public delegate void SlotDropRequestedEventHandler(int targetIndex, int unitTypeValue, int fromSlotIndex);

    [Export] public int SlotIndex = 0;

    [Export] private TextureRect _icon = null!;
    [Export] private Label _nameLabel = null!;
    [Export] private Label _costLabel = null!;
    [Export] private Label _dragPreviewTemplate = null!;
    private UnitType? _unitType;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_nameLabel, nameof(_nameLabel));
        this.EnsureNotNull(_costLabel, nameof(_costLabel));
        this.EnsureNotNull(_dragPreviewTemplate, nameof(_dragPreviewTemplate));

        UpdateVisual();
    }

    public void SetUnit(UnitData unitData)
    {
        _unitType = unitData.Type;
        _icon.Texture = unitData.Icon;
        _nameLabel.Text = $"{SlotIndex + 1}. {unitData.Name}";
        _costLabel.Text = unitData.MatchCost.ToString();
        TooltipText = unitData.Name;
        UpdateVisual();
    }

    public void ClearUnit()
    {
        _unitType = null;
        _icon.Texture = null;
        _nameLabel.Text = $"{SlotIndex + 1}. [Empty]";
        _costLabel.Text = "-";
        TooltipText = "";
        UpdateVisual();
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

        var preview = _dragPreviewTemplate.Duplicate() as Label;
        if (preview == null)
        {
            return default;
        }

        preview.Text = _nameLabel.Text;
        preview.Modulate = new Color(1f, 1f, 1f, 0.85f);
        SetDragPreview(preview);

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
        SelfModulate = _unitType.HasValue
            ? Colors.White
            : new Color(1f, 1f, 1f, 0.65f);
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
