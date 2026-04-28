using Godot;
using System.Threading.Tasks;
using System;

namespace FarmDefenseHarvestWars.GameClient._Autoload;

public partial class ProfilePoller : Node
{
    public static ProfilePoller Instance { get; private set; } = null!;

    [Export] public double PollIntervalSeconds = 10.0;
    
    private double _timer = 0;
    private bool _isPolling = false;

    public override void _Ready()
    {
        Instance = this;
        // Start polling a bit early after login, but wait for first cycle
        _timer = PollIntervalSeconds - 2.0; 
    }

    public override void _Process(double delta)
    {
        // Only poll if logged in and not in a match
        if (GameState.Instance == null || !GameState.Instance.IsLoggedIn || _isPolling)
        {
            _timer = 0;
            return;
        }

        // We can skip polling if the game is active
        // For simplicity, we check if the active scene is the main menu
        if (GetTree().CurrentScene != null && GetTree().CurrentScene.Name != "MainMenu")
        {
            // If we are in Gameplay scene, we might want to stop polling to save bandwidth
            // Since gameplay uses its own networking.
            return;
        }

        _timer += delta;
        if (_timer >= PollIntervalSeconds)
        {
            _timer = 0;
            _ = PollProfileAsync();
        }
    }

    private async Task PollProfileAsync()
    {
        if (NetworkBootstrap.Instance?.Menu == null) return;

        _isPolling = true;
        try
        {
            // GD.Print("[ProfilePoller] Polling profile update...");
            await NetworkBootstrap.Instance.Menu.GetProfileAsync();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ProfilePoller] Failed to poll profile: {ex.Message}");
        }
        finally
        {
            _isPolling = false;
        }
    }

    public void ResetTimer()
    {
        _timer = 0;
    }
}
