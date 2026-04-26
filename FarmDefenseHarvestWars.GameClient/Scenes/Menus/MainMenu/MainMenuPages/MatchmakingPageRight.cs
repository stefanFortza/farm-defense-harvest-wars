using Godot;
using System;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class MatchmakingPageRight : MarginContainer
{
    [Export] private Label _emailLabel = null!;
    [Export] private Label _levelLabel = null!;
    [Export] private Label _goldLabel = null!;
    [Export] private TextureRect _avatarTexture = null!;
    [Export] private Button _prevBtn = null!;
    [Export] private Button _nextBtn = null!;

    [Export] private Texture2D[] _avatars = Array.Empty<Texture2D>();

    public override void _Ready()
    {
        UpdateUI();
        if (GameState.Instance != null)
        {
            GameState.Instance.ProfileUpdated += UpdateUI;
        }

        if (_prevBtn != null) _prevBtn.Pressed += () => ChangeAvatar(-1);
        if (_nextBtn != null) _nextBtn.Pressed += () => ChangeAvatar(1);
    }

    private void ChangeAvatar(int direction)
    {
        if (GameState.Instance?.CurrentProfile == null) return;

        int currentIndex = GameState.Instance.CurrentProfile.AvatarIndex;
        int nextIndex = currentIndex + direction;

        if (nextIndex < 1) nextIndex = 8;
        if (nextIndex > 8) nextIndex = 1;

        OnAvatarSelected(nextIndex);
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

        if (_avatarTexture != null && _avatars.Length >= profile.AvatarIndex)
        {
            _avatarTexture.Texture = _avatars[profile.AvatarIndex - 1];
        }
    }

    private async void OnAvatarSelected(int index)
    {
        GD.Print($"[MatchmakingPageRight] Selecting avatar: {index}");
        try
        {
            await NetworkBootstrap.Instance.Menu.UpdateAvatarAsync(index);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MatchmakingPageRight] Failed to update avatar: {ex.Message}");
        }
    }

    private void SetPlaceholderValues()
    {
        if (_emailLabel != null) _emailLabel.Text = "Not Logged In";
        if (_levelLabel != null) _levelLabel.Text = "Level: --";
        if (_goldLabel != null) _goldLabel.Text = "Gold: --";
    }
}
