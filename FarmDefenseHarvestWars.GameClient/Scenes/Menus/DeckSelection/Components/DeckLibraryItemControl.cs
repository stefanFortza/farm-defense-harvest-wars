using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

public partial class DeckLibraryItemControl : PanelContainer
{
    [Export] private TextureRect _icon = null!;
    [Export] private Label _label = null!;
    [Export] private PackedScene _dragPreviewScene = null!;
    private int _unitTypeValue;
    private bool _canDrag;
    private bool _isUnlocked;
    private bool _isUnlocking;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_label, nameof(_label));
        this.EnsureNotNull(_dragPreviewScene, nameof(_dragPreviewScene));

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public void Setup(UnitData unitData, bool alreadyInDeck, bool isUnlocked, bool isUnlocking, bool isDeckSaving)
    {
        _unitTypeValue = (int)unitData.Type;
        _isUnlocked = isUnlocked;
        _isUnlocking = isUnlocking;
        _canDrag = !alreadyInDeck && isUnlocked && !isUnlocking && !isDeckSaving;

        _icon.Texture = unitData.Icon;
        string deckTag = alreadyInDeck ? " [IN DECK]" : "";
        string lockTag = isUnlocked ? "" : " [LOCKED]";
        string pendingTag = isUnlocking ? " [UNLOCKING...]" : "";
        _label.Text = $"{unitData.Name}{deckTag}{lockTag}{pendingTag}";
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
            : isDeckSaving
                ? CursorShape.Busy
            : isUnlocking
                ? CursorShape.Busy
                : isUnlocked
                ? CursorShape.Forbidden
                : CursorShape.PointingHand;

        // GD.Print($"Setup library item: {unitData.Name}, Unlocked: {isUnlocked}, Unlocking: {isUnlocking}, InDeck: {alreadyInDeck}, CanDrag: {_canDrag}");
    }

    public override void _ExitTree()
    {
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
    }

    public void OnMouseEntered()
    {
        if (!_isUnlocked && !_isUnlocking)
        {
            return;
        }

        UIAnimations.TryAnimateScaleUp(this, .2f);
    }

    public void OnMouseExited()
    {
        if (!_isUnlocked && !_isUnlocking)
        {
            return;
        }

        UIAnimations.TryAnimateScaleDown(this, .2f);
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
