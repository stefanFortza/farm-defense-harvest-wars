using Godot;
using System;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class MatchmakingPageRight : MarginContainer
{
    [Export] private Label _emailLabel = null!;
    [Export] private Label _levelLabel = null!;
    [Export] private Label _goldLabel = null!;

    public override void _Ready()
    {
        UpdateUI();
        if (GameState.Instance != null)
        {
            GameState.Instance.ProfileUpdated += UpdateUI;
        }
    }

    public override void _ExitTree()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.ProfileUpdated -= UpdateUI;
        }
    }

    private void UpdateUI()
    {
        if (GameState.Instance == null || !GameState.Instance.IsLoggedIn || GameState.Instance.CurrentProfile == null)
        {
            SetPlaceholderValues();
            return;
        }

        var profile = GameState.Instance.CurrentProfile;

        if (_emailLabel != null) _emailLabel.Text = profile.Email;
        if (_levelLabel != null) _levelLabel.Text = $"Level: {profile.Level}";
        if (_goldLabel != null) _goldLabel.Text = $"Gold: {profile.Gold}";
    }

    private void SetPlaceholderValues()
    {
        if (_emailLabel != null) _emailLabel.Text = "Not Logged In";
        if (_levelLabel != null) _levelLabel.Text = "Level: --";
        if (_goldLabel != null) _goldLabel.Text = "Gold: --";
    }
}
