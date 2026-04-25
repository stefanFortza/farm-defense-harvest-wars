using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

namespace FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;

public partial class UnitTooltip : PanelContainer
{
    [Export] private Label _nameLabel = null!;
    [Export] private Label _costLabel = null!;
    [Export] private Label _healthLabel = null!;
    [Export] private Label _damageLabel = null!;
    [Export] private Label _rangeLabel = null!;
    [Export] private Label _speedLabel = null!;
    [Export] private TextureRect _iconRect = null!;

    public override void _Ready()
    {
        // Custom smooth appearance animation
        Modulate = new Color(1, 1, 1, 0);
        
        // Use a slight delay to allow size calculation if needed, 
        // but for tooltips we usually want immediate feedback.
        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetEase(Tween.EaseType.Out);
        
        // Fade in
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.15f);
        
        // Slight scale up from 0.95 to 1.0 for a "zoom" feel instead of a "bounce"
        Scale = new Vector2(0.95f, 0.95f);
        PivotOffset = Size / 2;
        tween.TweenProperty(this, "scale", Vector2.One, 0.15f);
    }

    public void Setup(UnitData data)
    {
        _nameLabel.Text = data.Name;
        _costLabel.Text = data.MatchCost.ToString();
        _healthLabel.Text = data.MaxHealth.ToString();
        _damageLabel.Text = data.Damage.ToString();
        _rangeLabel.Text = data.AttackRange.ToString();
        _speedLabel.Text = data.Speed > 0 ? data.Speed.ToString() : "Static";

        if (data.Icon != null)
        {
            _iconRect.Texture = data.Icon;
        }
    }
}
