using Godot;
using FarmDefenseHarvestWars.Shared.Models.Game;
using System;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

public partial class ChestSlotControl : PanelContainer
{
    [Export] private TextureRect _icon = null!;
    [Export] private Label _emptyLabel = null!;
    [Export] private Texture2D _chestTexture = null!;
    [Export] private PackedScene _chestRewardPopupScene = null!;
    
    private ChestDto? _chest;

    public override void _Ready()
    {
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_emptyLabel, nameof(_emptyLabel));
        
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public void Setup(ChestDto? chest)
    {
        _chest = chest;
        if (chest != null)
        {
            _icon.Texture = _chestTexture;
            _icon.Show();
            _emptyLabel.Hide();
            TooltipText = $"Chest: {chest.Name}\nAcquired: {chest.AcquiredAt.ToLocalTime():g}\nClick to open!";
            MouseDefaultCursorShape = CursorShape.PointingHand;
        }
        else
        {
            _icon.Hide();
            _emptyLabel.Show();
            TooltipText = "Empty Slot";
            MouseDefaultCursorShape = CursorShape.Arrow;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (_chest != null)
            {
                OpenChest();
            }
        }
    }

    private async void OpenChest()
    {
        if (_chest == null) return;
        
        try
        {
            var result = await NetworkBootstrap.Instance.Menu.OpenChestAsync(_chest.Id);
            
            if (_chestRewardPopupScene != null)
            {
                var popup = _chestRewardPopupScene.Instantiate<ChestRewardPopup>();
                GetTree().Root.AddChild(popup);
                popup.Setup(result.Rewards);
            }

            GD.Print($"Opened chest! Found {result.Rewards.Count} rewards.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to open chest: {ex.Message}");
        }
    }

    private void OnMouseEntered()
    {
        if (_chest != null)
        {
            UIAnimations.TryAnimateScaleUp(this, 0.1f);
        }
    }

    private void OnMouseExited()
    {
        UIAnimations.TryAnimateScaleDown(this, 0.1f);
    }
}
