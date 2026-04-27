using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FarmDefenseHarvestWars.Shared.Enums;
using System;

public partial class UpgradePopup : CanvasLayer
{
    [Export] private TextureRect _unitIcon = null!;
    [Export] private Label _unitName = null!;
    [Export] private Label _levelLabel = null!;
    [Export] private Label _fragmentsLabel = null!;
    [Export] private ProgressBar _fragmentsBar = null!;
    [Export] private Label _costLabel = null!;
    [Export] private Button _upgradeButton = null!;
    [Export] private Button _closeButton = null!;

    private UnitData? _unitData;
    private UnitUnlockDto? _unlock;

    public override void _Ready()
    {
        this.EnsureNotNull(_unitIcon, nameof(_unitIcon));
        this.EnsureNotNull(_unitName, nameof(_unitName));
        this.EnsureNotNull(_levelLabel, nameof(_levelLabel));
        this.EnsureNotNull(_fragmentsLabel, nameof(_fragmentsLabel));
        this.EnsureNotNull(_fragmentsBar, nameof(_fragmentsBar));
        this.EnsureNotNull(_costLabel, nameof(_costLabel));
        this.EnsureNotNull(_upgradeButton, nameof(_upgradeButton));
        this.EnsureNotNull(_closeButton, nameof(_closeButton));

        _closeButton.Pressed += () => QueueFree();
        _upgradeButton.Pressed += OnUpgradePressed;

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

    public void Setup(UnitData unitData, UnitUnlockDto unlock)
    {
        _unitData = unitData;
        _unlock = unlock;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_unitData == null || _unlock == null) return;

        _unitIcon.Texture = _unitData.Icon;
        _unitName.Text = _unitData.Name;
        _levelLabel.Text = $"Level {_unlock.Level}";

        int required = _unlock.FragmentsRequiredForNextLevel;
        _fragmentsLabel.Text = $"{_unlock.Fragments} / {required}";
        _fragmentsBar.MaxValue = required;
        _fragmentsBar.Value = Math.Min(_unlock.Fragments, required);

        _costLabel.Text = $"{_unlock.UpgradeCost} Gold";

        bool canAfford = GameState.Instance?.CurrentProfile?.Gold >= _unlock.UpgradeCost;
        bool hasFragments = _unlock.Fragments >= required;

        _upgradeButton.Disabled = !canAfford || !hasFragments;

        if (!hasFragments)
            _upgradeButton.TooltipText = "Not enough fragments!";
        else if (!canAfford)
            _upgradeButton.TooltipText = "Not enough gold!";
        else
            _upgradeButton.TooltipText = "Ready to upgrade!";
    }

    private async void OnUpgradePressed()
    {
        if (_unitData == null || _unlock == null) return;

        _upgradeButton.Disabled = true;
        try
        {
            var newProfile = await NetworkBootstrap.Instance.Menu.UpgradeUnitAsync(_unitData.Type);
            // After upgrade, find the new unlock data for this unit
            var role = _unitData.Role;
            var newUnlock = GameState.Instance?.GetUnitUnlock(role, _unitData.Type);

            if (newUnlock != null)
            {
                _unlock = newUnlock;
                UpdateUI();
                GD.Print($"Successfully upgraded {_unitData.Name} to level {_unlock.Level}!");
            }
            else
            {
                QueueFree();
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Upgrade failed: {ex.Message}");
            _upgradeButton.Disabled = false;
        }
    }
}
