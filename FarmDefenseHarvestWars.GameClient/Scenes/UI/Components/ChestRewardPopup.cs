using Godot;
using FarmDefenseHarvestWars.Shared.Models.Game;
using System.Collections.Generic;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

public partial class ChestRewardPopup : CanvasLayer
{
    [Export] private Container _rewardContainer = null!;
    [Export] private PackedScene _rewardItemScene = null!;
    [Export] private Button _closeButton = null!;

    public override void _Ready()
    {
        this.EnsureNotNull(_rewardContainer, nameof(_rewardContainer));
        this.EnsureNotNull(_rewardItemScene, nameof(_rewardItemScene));
        this.EnsureNotNull(_closeButton, nameof(_closeButton));

        _closeButton.Pressed += () => QueueFree();

        // Background click to close
        var backgroundControl = GetNodeOrNull<Control>("Control");
        if (backgroundControl != null)
        {
            backgroundControl.GuiInput += (ev) => {
                if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                    QueueFree();
            };
        }
    }

    public void Setup(List<UnitUnlockDto> rewards)
    {
        foreach (var child in _rewardContainer.GetChildren())
            child.QueueFree();

        foreach (var reward in rewards)
        {
            var item = _rewardItemScene.Instantiate<ChestRewardItemControl>();
            _rewardContainer.AddChild(item);
            item.Setup(reward);
        }
    }
}
