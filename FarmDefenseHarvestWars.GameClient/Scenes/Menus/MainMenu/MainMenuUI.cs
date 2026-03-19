using Godot;

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
        // Ensure correct initial state
        CloseAllOverlays();

        _matchmakingTimer = new Timer
        {
            WaitTime = 2.0f, // Simulate 2 seconds search
            OneShot = true
        };
        _matchmakingTimer.Timeout += OnMatchFound;
        AddChild(_matchmakingTimer);

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
            WelcomeLabel?.Text = $"Welcome, {profile.Email}!";
            GoldLabel?.Text = $"{profile.Gold}";
            LevelLabel?.Text = $"Level: {profile.Level}";
        }
        else
        {
            WelcomeLabel?.Text = "Not logged in.";
        }
    }

    private void CloseAllOverlays()
    {
        MatchmakingPanel?.Visible = false;
        SettingsPanel?.Visible = false;
        MenuButtons?.Visible = true;
    }

    // --- Button Handlers ---

    public void OnFindMatchPressed()
    {
        // DEBUG MODE: Connect directly to localhost
        // In Phase 4, this will be replaced by the Matchmaking API call

        // 1. Connect to local server
        // NetworkManager.Instance.JoinGame("127.0.0.1");
        NetworkBootstrap.Instance.Gameplay.JoinGameServer("127.0.0.1");

        // 2. Change scene immediately
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");

        /* 
        // OLD SIMULATION CODE (Commented out for Phase 2)
        if (MatchmakingPanel != null) MatchmakingPanel.Visible = true;
        _matchmakingTimer.Start();
        */
    }

    public void OnSettingsPressed()
    {
        SettingsPanel?.Visible = true;
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
        SettingsPanel?.Visible = false;
    }

    public void OnCancelMatchmakingPressed()
    {
        MatchmakingPanel?.Visible = false;
        _matchmakingTimer.Stop();
    }

    // Placeholder for when match is found
    public void OnMatchFound()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
    }
}