using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;

public partial class UnitTooltip : MarginContainer
{
    [Export] private Label _nameLabel = null!;
    [Export] private Label _costLabel = null!;
    [Export] private Label _healthLabel = null!;
    [Export] private Label _damageLabel = null!;
    [Export] private Label _rangeLabel = null!;
    [Export] private Label _speedLabel = null!;
    [Export] private TextureRect _iconRect = null!;
    [Export] private Label _levelLabel = null!;

    public override void _Ready()
    {
    }

    public void Setup(UnitData data, UnitUnlockDto? unlock = null)
    {
        _nameLabel.Text = data.Name;
        
        if (_levelLabel != null)
        {
            // If we have explicit unlock data, use it. 
            // Otherwise, if it's default unlocked or we know it's unlocked from context, show Lvl 1
            bool isUnlocked = unlock != null || data.IsDefaultUnlocked;
            
            if (isUnlocked)
            {
                int level = unlock?.Level ?? 1;
                _levelLabel.Text = $"Lvl {level}";
                _levelLabel.Show();
            }
            else
            {
                _levelLabel.Hide();
            }
        }

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
