using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using System.Threading;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

namespace FarmDefenseHarvestWars.GameClient.Scenes.UI;

public partial class UnitCard : PanelContainer
{
    [Signal] public delegate void CardPressedEventHandler(int unitType);

    [Export] private TextureRect _icon = null!;
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
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_costLabel, nameof(_costLabel));
        this.EnsureNotNull(_cooldownOverlay, nameof(_cooldownOverlay));
        this.EnsureNotNull(_cooldownLabel, nameof(_cooldownLabel));

        SetProcess(false);
        UpdateVisualState();
    }

    public void Setup(UnitData data)
    {
        _data = data;
        _icon.Texture = data.Icon;
        _costLabel.Text = data.MatchCost.ToString();
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

        _cooldownOverlay.Visible = onCooldown;

        _cooldownLabel.Visible = onCooldown;
        _cooldownLabel.Text = Mathf.CeilToInt(_cooldownRemaining).ToString();
    }
}
