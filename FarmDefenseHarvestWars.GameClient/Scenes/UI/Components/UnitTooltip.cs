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
        // Apply popup animation
        UIAnimations.AnimatePop(this, 0.25f);
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
