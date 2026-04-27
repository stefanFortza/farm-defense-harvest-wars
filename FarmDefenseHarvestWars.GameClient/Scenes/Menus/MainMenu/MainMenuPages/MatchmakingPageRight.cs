using Godot;
using System;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.Shared.Models.Game;
using System.Collections.Generic;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class MatchmakingPageRight : MarginContainer
{
    [Export] private Label _emailLabel = null!;
    [Export] private Label _levelLabel = null!;
    [Export] private Label _goldLabel = null!;
    [Export] private TextureRect _avatarTexture = null!;
    [Export] private Container _chestContainer = null!;
    [Export] private PackedScene _chestSlotScene = null!;
    [Export] private Button _prevBtn = null!;
    [Export] private Button _nextBtn = null!;

    [Export] private Texture2D[] _avatars = Array.Empty<Texture2D>();

    private readonly List<ChestSlotControl> _chestSlots = new();

    public override void _Ready()
    {
        _chestContainer ??= GetNodeOrNull<Container>("VBox/ChestContainer");
        if (_chestSlotScene == null && ResourceLoader.Exists("res://Scenes/UI/Components/ChestSlotControl.tscn"))
        {
            _chestSlotScene = GD.Load<PackedScene>("res://Scenes/UI/Components/ChestSlotControl.tscn");
        }

        InitializeChestSlots();
        UpdateUI();
        if (GameState.Instance != null)
        {
            GameState.Instance.ProfileUpdated += UpdateUI;
        }

        _prevBtn.MouseEntered += OnMouseEnteredPrev;
        _prevBtn.MouseExited += OnMouseExitedPrev;
        _nextBtn.MouseEntered += OnMouseEnteredNext;
        _nextBtn.MouseExited += OnMouseExitedNext;

        if (_prevBtn != null) _prevBtn.Pressed += () => ChangeAvatar(-1);
        if (_nextBtn != null) _nextBtn.Pressed += () => ChangeAvatar(1);
    }

    private void OnMouseEnteredPrev()
    {
        UIAnimations.TryAnimateScale(_prevBtn, new Vector2(1.05f, 1.05f), 0.15);
        AnimateHoverShader(1.0f);
    }

    private void OnMouseExitedPrev()
    {
        UIAnimations.TryAnimateScale(_prevBtn, Vector2.One, 0.15);
        AnimateHoverShader(0.0f);
    }

    public void OnMouseEnteredNext()
    {
        UIAnimations.TryAnimateScale(_nextBtn, new Vector2(1.05f, 1.05f), 0.15);
        AnimateHoverShader(1.0f);
    }

    public void OnMouseExitedNext()
    {
        UIAnimations.TryAnimateScale(_nextBtn, Vector2.One, 0.15);
        AnimateHoverShader(0.0f);
    }


    private void AnimateHoverShader(float target)
    {
        if (Material is ShaderMaterial shaderMat)
        {
            var tween = GetTree().CreateTween();
            tween.TweenMethod(Callable.From<float>((val) => shaderMat.SetShaderParameter("hover_intensity", val)),
                (float)shaderMat.GetShaderParameter("hover_intensity"), target, 0.15);
        }
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
            GD.Print($"[MatchmakingPageRight] Setting avatar index: {profile.AvatarIndex}");
            _avatarTexture.Texture = _avatars[profile.AvatarIndex];
        }

        UpdateChestSlots(profile.Chests);
    }

    private void InitializeChestSlots()
    {
        if (_chestContainer == null || _chestSlotScene == null) return;

        foreach (var child in _chestContainer.GetChildren())
        {
            child.QueueFree();
        }

        _chestSlots.Clear();
        for (int i = 0; i < 5; i++)
        {
            var slot = _chestSlotScene.Instantiate<ChestSlotControl>();
            _chestContainer.AddChild(slot);
            _chestSlots.Add(slot);
            slot.Setup(null);
        }
    }

    private void UpdateChestSlots(IReadOnlyList<ChestDto> chests)
    {
        for (int i = 0; i < _chestSlots.Count; i++)
        {
            var chest = i < chests.Count ? chests[i] : null;
            _chestSlots[i].Setup(chest);
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
