using Godot;
using System;
using Refit;

public partial class MainMenuUI : Control
{
    [Export] public Control MatchmakingPanel { get; set; }
    [Export] public Control SettingsPanel { get; set; }
    [Export] public Control MenuButtons { get; set; }

    // Restored UI Elements
    [Export] public Label WelcomeLabel { get; set; }
    [Export] public Label GoldLabel { get; set; }
    [Export] public Label LevelLabel { get; set; }

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

    public async void OnFindMatchPressed()
    {
        if (NetworkBootstrap.Instance.Menu.IsMatchmakingActive)
        {
            return;
        }

        MatchmakingPanel?.Show();

        try
        {
            var status = await NetworkBootstrap.Instance.Menu.StartMatchmakingUntilFoundAsync();
            if (status == null || !status.MatchFound)
            {
                return;
            }

            string host = string.IsNullOrWhiteSpace(status.ServerAddress) ? "127.0.0.1" : status.ServerAddress;
            int port = status.ServerPort ?? 7777;

            NetworkBootstrap.Instance.Gameplay.JoinGameServer(host, port);
            GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
        }
        catch (ApiException ex)
        {
            GD.PrintErr($"Matchmaking failed: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            GD.PrintErr($"Matchmaking state error: {ex.Message}");
        }
        finally
        {
            MatchmakingPanel?.Hide();
        }
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