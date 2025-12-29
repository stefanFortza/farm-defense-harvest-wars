using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class MainMenuUI : Control
{
    [Export] public Control MatchmakingPanel { get; set; }
    [Export] public Control SettingsPanel { get; set; }
    [Export] public Control MenuButtons { get; set; }

    // Restored UI Elements
    [Export] public Label WelcomeLabel { get; set; }
    [Export] public Label GoldLabel { get; set; }
    [Export] public Label LevelLabel { get; set; }

    private Timer _matchmakingTimer;

    public override void _Ready()
    {
        // 1. Check launch args
        string[] args = OS.GetCmdlineArgs();

        // 2. Check for "--server"
        if (args.Contains("--server"))
        {
            GD.Print("Argument '--server' detected! Starting auto server...");
            StartAutoServer();
            return; // Stop rest of initialization
        }

        // Ensure correct initial state
        CloseAllOverlays();

        _matchmakingTimer = new Timer();
        _matchmakingTimer.WaitTime = 2.0f; // Simulate 2 seconds search
        _matchmakingTimer.OneShot = true;
        _matchmakingTimer.Timeout += OnMatchFound;
        AddChild(_matchmakingTimer);

        // Update UI with GameState
        UpdateUI();
        if (GameState.Instance != null)
        {
            GameState.Instance.ProfileUpdated += UpdateUI;
        }
    }

    private async Task StartAutoServer()
    {
        // Start server
        NetworkManager.Instance.StartServer();

        // Defer the frame to ensure server is fully initialized

        // Change scene immediately to GameWorld
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld.tscn");
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
        if (GameState.Instance != null && GameState.Instance.IsLoggedIn && GameState.Instance.CurrentProfile != null)
        {
            var profile = GameState.Instance.CurrentProfile;
            if (WelcomeLabel != null) WelcomeLabel.Text = $"Welcome, {profile.Email}!";
            if (GoldLabel != null) GoldLabel.Text = $"{profile.Gold}";
            if (LevelLabel != null) LevelLabel.Text = $"Level: {profile.Level}";
        }
        else
        {
            if (WelcomeLabel != null) WelcomeLabel.Text = "Not logged in.";
        }
    }

    private void CloseAllOverlays()
    {
        if (MatchmakingPanel != null) MatchmakingPanel.Visible = false;
        if (SettingsPanel != null) SettingsPanel.Visible = false;
        if (MenuButtons != null) MenuButtons.Visible = true;
    }

    // --- Button Handlers ---

    public void OnFindMatchPressed()
    {
        // DEBUG MODE: Connect directly to localhost
        // In Phase 4, this will be replaced by the Matchmaking API call

        // 1. Connect to local server
        NetworkManager.Instance.JoinGame("127.0.0.1");

        // 2. Change scene immediately
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld.tscn");

        /* 
        // OLD SIMULATION CODE (Commented out for Phase 2)
        if (MatchmakingPanel != null) MatchmakingPanel.Visible = true;
        _matchmakingTimer.Start();
        */
    }

    public void OnSettingsPressed()
    {
        if (SettingsPanel != null) SettingsPanel.Visible = true;
    }

    public void OnLogoutPressed()
    {
        NetworkManager.Instance.Logout();
        GetTree().ChangeSceneToFile("res://Scenes/Authentication/AuthScene.tscn");
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }

    public void OnCloseSettingsPressed()
    {
        if (SettingsPanel != null) SettingsPanel.Visible = false;
    }

    public void OnCancelMatchmakingPressed()
    {
        if (MatchmakingPanel != null) MatchmakingPanel.Visible = false;
        _matchmakingTimer.Stop();
    }

    // Placeholder for when match is found
    public void OnMatchFound()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld.tscn");
    }
}