using Godot;
using System;
using Refit;

public partial class MainMenuUI : Control
{
    private const string ProfileWelcomeGroup = "profile_welcome_label";
    private const string ProfileGoldGroup = "profile_gold_label";
    private const string ProfileLevelGroup = "profile_level_label";

    [Export] public Control? MatchmakingPanel { get; set; } = null!;
    [Export] public Control? SettingsPanel { get; set; } = null!;
    [Export] public Control? MenuButtons { get; set; } = null!;

    // Restored UI Elements
    [Export] public Label? WelcomeLabel { get; set; } = null!;
    [Export] public Label? GoldLabel { get; set; } = null!;
    [Export] public Label? LevelLabel { get; set; } = null!;

    public override void _Ready()
    {
        // Ensure correct initial state
        CloseAllOverlays();

        // Update UI with GameState
        UpdateUI();
        GameState.Instance?.ProfileUpdated += UpdateUI;
    }

    public override void _ExitTree()
    {
        GameState.Instance?.ProfileUpdated -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (GameState.Instance != null && GameState.Instance.IsLoggedIn && GameState.Instance.CurrentProfile != null)
        {
            var profile = GameState.Instance.CurrentProfile;
            string welcomeText = $"Welcome, {profile.Email}!";
            string goldText = $"Gold: {profile.Gold}";
            string levelText = $"Level: {profile.Level}";

            WelcomeLabel?.Text = welcomeText;
            GoldLabel?.Text = goldText;
            LevelLabel?.Text = levelText;

            SetLabelsInGroup(ProfileWelcomeGroup, welcomeText);
            SetLabelsInGroup(ProfileGoldGroup, goldText);
            SetLabelsInGroup(ProfileLevelGroup, levelText);
        }
        else
        {
            const string notLoggedText = "Not logged in.";

            WelcomeLabel?.Text = notLoggedText;
            GoldLabel?.Text = "Gold: --";
            LevelLabel?.Text = "Level: --";

            SetLabelsInGroup(ProfileWelcomeGroup, notLoggedText);
            SetLabelsInGroup(ProfileGoldGroup, "Gold: --");
            SetLabelsInGroup(ProfileLevelGroup, "Level: --");
        }
    }

    private void SetLabelsInGroup(string groupName, string text)
    {
        foreach (var node in GetTree().GetNodesInGroup(groupName))
        {
            if (node is Label label)
            {
                label.Text = text;
            }
        }
    }

    private void CloseAllOverlays()
    {
        MatchmakingPanel?.Visible = false;
        SettingsPanel?.Visible = false;
        MenuButtons?.Visible = true;
    }

    // --- Button Handlers ---

    public void OnSettingsPressed()
    {
        ActivateTabByKey("SettingsPage");
    }

    public void OnLogoutPressed()
    {
        NetworkBootstrap.Instance.Auth.Logout();
        GetTree().ChangeSceneToFile("res://Scenes/Authentication/AuthScene.tscn");
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }

    public void OnCloseSettingsPressed()
    {
        ActivateTabByKey("MatchMakingPage");
    }

    private void ActivateTabByKey(string tabKey)
    {
        var tabsLayer = GetNodeOrNull<Control>("MainMenuBookUI/TabsLayer");
        if (tabsLayer == null)
        {
            return;
        }

        foreach (Node child in tabsLayer.GetChildren())
        {
            if (child is TabButton tabButton && tabButton.TabKey == tabKey)
            {
                tabButton.ButtonPressed = true;
                return;
            }
        }
    }

    public async void OnCancelMatchmakingPressed()
    {
        MatchmakingPanel?.Visible = false;

        try
        {
            await NetworkBootstrap.Instance.Menu.CancelMatchmakingAsync();
        }
        catch (ApiException ex)
        {
            GD.PrintErr($"Failed to cancel matchmaking: {ex.Message}");
        }
    }

    // Placeholder for when match is found
    public void OnMatchFound()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
    }
}