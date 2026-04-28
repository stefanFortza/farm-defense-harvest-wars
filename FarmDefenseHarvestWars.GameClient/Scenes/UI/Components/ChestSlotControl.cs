using Godot;
using FarmDefenseHarvestWars.Shared.Models.Game;
using System;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

public partial class ChestSlotControl : PanelContainer
{
    [Export] private TextureRect _icon = null!;
    [Export] private Label _statusLabel = null!; // Renamed from _emptyLabel for clarity in code
    
    [ExportGroup("Chest Textures")]
    [Export] private Texture2D _woodTexture = null!;
    [Export] private Texture2D _silverTexture = null!;
    [Export] private Texture2D _goldTexture = null!;
    
    [Export] private PackedScene _chestRewardPopupScene = null!;
    
    private ChestDto? _chest;
    private double _updateTimer = 0.0;

    public override void _Ready()
    {
        this.EnsureNotNull(_icon, nameof(_icon));
        this.EnsureNotNull(_statusLabel, nameof(_statusLabel));
        
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public void Setup(ChestDto? chest)
    {
        _chest = chest;
        UpdateUI();
    }

    public override void _Process(double delta)
    {
        if (_chest != null && _chest.UnlockStartTime.HasValue)
        {
            _updateTimer += delta;
            if (_updateTimer >= 1.0)
            {
                _updateTimer = 0.0;
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        if (_chest != null)
        {
            _icon.Texture = GetTextureForChest(_chest.Name);
            _icon.Show();
            _statusLabel.Show();
            MouseDefaultCursorShape = CursorShape.PointingHand;

            if (_chest.UnlockStartTime.HasValue)
            {
                var unlockTimeElapsed = DateTime.UtcNow - _chest.UnlockStartTime.Value;
                var remaining = _chest.UnlockDurationSeconds - unlockTimeElapsed.TotalSeconds;

                if (remaining <= 0)
                {
                    _statusLabel.Text = "OPEN";
                    TooltipText = $"Chest: {_chest.Name}\nReady to open!";
                }
                else
                {
                    _statusLabel.Text = FormatTime(remaining);
                    TooltipText = $"Chest: {_chest.Name}\nUnlocking...\nTime left: {FormatTime(remaining)}";
                }
            }
            else
            {
                _statusLabel.Text = "UNLOCK";
                TooltipText = $"Chest: {_chest.Name}\nClick to start unlocking ({FormatTime(_chest.UnlockDurationSeconds)})";
            }
        }
        else
        {
            _icon.Hide();
            _statusLabel.Text = "EMPTY";
            _statusLabel.Show();
            TooltipText = "Empty Slot";
            MouseDefaultCursorShape = CursorShape.Arrow;
        }
    }

    private Texture2D GetTextureForChest(string name)
    {
        if (string.IsNullOrEmpty(name)) return _woodTexture;
        
        if (name.Contains("Gold", StringComparison.OrdinalIgnoreCase)) return _goldTexture;
        if (name.Contains("Silver", StringComparison.OrdinalIgnoreCase)) return _silverTexture;
        
        return _woodTexture;
    }

    private string FormatTime(double seconds)
    {
        if (seconds < 0) seconds = 0;
        var t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours}h {t.Minutes}m";
        if (t.TotalMinutes >= 1)
            return $"{t.Minutes}m {t.Seconds}s";
        return $"{t.Seconds}s";
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (_chest != null)
            {
                HandleChestClick();
            }
        }
    }

    private async void HandleChestClick()
    {
        if (_chest == null) return;

        if (!_chest.UnlockStartTime.HasValue)
        {
            // Start Unlock
            try
            {
                await NetworkBootstrap.Instance.Menu.StartUnlockChestAsync(_chest.Id);
                GD.Print("Started unlocking chest.");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to start unlock: {ex.Message}");
                // Show red toast notification
                ToastNotifications.TryError("Another chest is already unlocking!", 2.5);
                
                // Optional: Animate a "shake" effect to indicate error
                UIAnimations.TryAnimateScale(this, new Vector2(1.1f, 1.1f), 0.1);
                var tween = GetTree().CreateTween();
                tween.TweenProperty(this, "modulate", new Color(1, 0.5f, 0.5f), 0.1);
                tween.TweenProperty(this, "modulate", new Color(1, 1, 1), 0.1);
            }
        }
        else
        {
            var unlockTimeElapsed = DateTime.UtcNow - _chest.UnlockStartTime.Value;
            if (unlockTimeElapsed.TotalSeconds >= _chest.UnlockDurationSeconds)
            {
                OpenChest();
            }
            else
            {
                GD.Print("Chest is still unlocking...");
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
