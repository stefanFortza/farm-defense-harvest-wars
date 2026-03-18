using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

public partial class Card : PanelContainer
{
    [Signal] public delegate void CardPressedEventHandler(int unitType);

    [Export] private TextureRect _icon = null!;
    [Export] private Label _nameLabel = null!;
    [Export] private Label _costLabel = null!;
    [Export] private ColorRect _cooldownOverlay = null!;
    [Export] private Label _cooldownLabel = null!;

    private UnitData _data = null!;
    private float _cooldownRemaining;
    private bool _isAffordable = true;

    public UnitType UnitType => _data?.Type ?? UnitType.None;
    public int MatchCost => _data?.MatchCost ?? int.MaxValue;

    public override void _Ready()
    {
        _icon ??= GetNodeOrNull<TextureRect>("Margin/VBox/Icon");
        _nameLabel ??= GetNodeOrNull<Label>("Margin/VBox/Name");
        _costLabel ??= GetNodeOrNull<Label>("Margin/VBox/CostRow/CostLabel");
        _cooldownOverlay ??= GetNodeOrNull<ColorRect>("CooldownOverlay");
        _cooldownLabel ??= GetNodeOrNull<Label>("CooldownOverlay/CooldownLabel");

        SetProcess(false);
        UpdateVisualState();
    }

    public void Setup(UnitData data)
    {
        _data = data;

        if (_icon != null)
        {
            _icon.Texture = data.Icon;
        }

        if (_nameLabel != null)
        {
            _nameLabel.Text = data.Name;
        }

        if (_costLabel != null)
        {
            _costLabel.Text = data.MatchCost.ToString();
        }

        TooltipText = $"{data.Name} ({data.MatchCost})";
        UpdateVisualState();
    }

    public void SetAffordable(bool isAffordable)
    {
        _isAffordable = isAffordable;
        UpdateVisualState();
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

        if (_data == null || _cooldownRemaining > 0f || !_isAffordable)
        {
            return;
        }

        EmitSignal(SignalName.CardPressed, (int)_data.Type);
    }

    public void StartCooldown()
    {
        if (_data == null)
        {
            return;
        }

        _cooldownRemaining = Mathf.Max(_data.CardCooldownSeconds, 0f);
        if (_cooldownRemaining > 0f)
        {
            SetProcess(true);
            UpdateVisualState();
        }
    }

    public override void _Process(double delta)
    {
        if (_cooldownRemaining <= 0f)
        {
            _cooldownRemaining = 0f;
            SetProcess(false);
            UpdateVisualState();
            return;
        }

        _cooldownRemaining -= (float)delta;
        if (_cooldownRemaining < 0f)
        {
            _cooldownRemaining = 0f;
        }

        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        bool onCooldown = _cooldownRemaining > 0f;
        bool isUsable = _isAffordable && !onCooldown;

        MouseDefaultCursorShape = isUsable ? CursorShape.PointingHand : CursorShape.Forbidden;
        SelfModulate = isUsable ? Colors.White : new Color(1f, 1f, 1f, 0.55f);

        if (_cooldownOverlay != null)
        {
            _cooldownOverlay.Visible = onCooldown;
        }

        if (_cooldownLabel != null)
        {
            _cooldownLabel.Visible = onCooldown;
            _cooldownLabel.Text = Mathf.CeilToInt(_cooldownRemaining).ToString();
        }
    }
}
