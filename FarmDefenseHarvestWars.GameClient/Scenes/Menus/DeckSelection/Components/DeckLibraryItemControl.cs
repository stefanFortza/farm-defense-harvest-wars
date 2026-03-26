using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

public partial class DeckLibraryItemControl : PanelContainer
{
    [Signal]
    public delegate void UnlockRequestedEventHandler(int unitTypeValue);

    [Export] private TextureRect _icon = null!;
    [Export] private Label _label = null!;
    [Export] private Label _costLabel = null!;
    [Export] private Label _dragPreviewTemplate = null!;
    private int _unitTypeValue;
    private bool _canDrag;
    private bool _isUnlocked;
    private bool _isUnlocking;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_label, nameof(_label));
        this.EnsureNotNull(_costLabel, nameof(_costLabel));
        this.EnsureNotNull(_dragPreviewTemplate, nameof(_dragPreviewTemplate));
    }

    public void Setup(UnitData unitData, bool alreadyInDeck, bool isUnlocked, bool isUnlocking)
    {
        _unitTypeValue = (int)unitData.Type;
        _isUnlocked = isUnlocked;
        _isUnlocking = isUnlocking;
        _canDrag = !alreadyInDeck && isUnlocked && !isUnlocking;

        _icon.Texture = unitData.Icon;
        string deckTag = alreadyInDeck ? " [IN DECK]" : "";
        string lockTag = isUnlocked ? "" : " [LOCKED]";
        string pendingTag = isUnlocking ? " [UNLOCKING...]" : "";
        _label.Text = $"{unitData.Name}{deckTag}{lockTag}{pendingTag}";
        _costLabel.Text = isUnlocked ? unitData.MatchCost.ToString() : $"U:{unitData.UnlockCost}";
        TooltipText = isUnlocked
            ? unitData.Name
            : isUnlocking
                ? $"Unlock in progress for {unitData.Name}"
                : $"Click to unlock {unitData.Name} for {unitData.UnlockCost} gold";
        SelfModulate = !isUnlocked
            ? new Color(1f, 1f, 1f, 0.55f)
            : alreadyInDeck
                ? new Color(1f, 1f, 1f, 0.75f)
                : Colors.White;

        MouseDefaultCursorShape = _canDrag
            ? CursorShape.Drag
            : isUnlocking
                ? CursorShape.Busy
                : isUnlocked
                ? CursorShape.Forbidden
                : CursorShape.PointingHand;
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

        EmitSignal(SignalName.UnlockRequested, _unitTypeValue);
        AcceptEvent();
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

        var preview = _dragPreviewTemplate.Duplicate() as Label;
        if (preview == null)
        {
            return default;
        }

        preview.Text = _label.Text;
        preview.Modulate = new Color(1f, 1f, 1f, 0.85f);
        SetDragPreview(preview);

        return payload;
    }
}
