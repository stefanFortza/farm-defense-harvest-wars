using Godot;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

public partial class ChestRewardItemControl : PanelContainer
{
    [Export] private TextureRect _icon = null!;
    [Export] private Label _amountLabel = null!;

    public void Setup(UnitUnlockDto reward)
    {
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_amountLabel, nameof(_amountLabel));

        var registry = GD.Load<UnitRegistry>("res://Resources/Units/UnitRegistry.tres");
        if (registry != null)
        {
            var unitData = registry.GetUnitData(reward.UnitType);
            if (unitData != null)
            {
                _icon.Texture = unitData.Icon;
            }
        }

        _amountLabel.Text = $"+{reward.Fragments}";
    }
}
