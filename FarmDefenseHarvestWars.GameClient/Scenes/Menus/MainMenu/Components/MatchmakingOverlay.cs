using Godot;
using System;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Refit;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.Components;

public partial class MatchmakingOverlay : CanvasLayer
{
    [Export] private Label _statusLabel = null!;
    [Export] private Control _spinner = null!;
    [Export] private Button _cancelButton = null!;
    [Export] private Control? _contentToHide;

    private bool _isClosing = false;

    public override void _Ready()
    {
        Visible = false;
        if (_spinner != null)
        {
            _spinner.PivotOffset = _spinner.Size / 2;
        }
    }

    public override void _Process(double delta)
    {
        if (Visible && _spinner != null)
        {
            _spinner.Rotation += (float)delta * 5.0f;
        }
    }

    public async Task StartSearch(PlayerRole role)
    {
        if (Visible) return;

        _isClosing = false;
        Visible = true;
        
        if (_contentToHide != null)
        {
            _contentToHide.Visible = false;
        }

        _statusLabel.Text = $"Searching as {role}...";
        _cancelButton.Disabled = false;

        try
        {
            var status = await NetworkBootstrap.Instance.Menu.StartMatchmakingUntilFoundAsync(role);

            if (_isClosing) return;

            if (status == null || !status.MatchFound)
            {
                ToastNotifications.TryInfo("No match found.", 3.0);
                Close();
                return;
            }

            _statusLabel.Text = "Match Found! Connecting...";
            ToastNotifications.TrySuccess("Match Found! Connecting...", 3.0);
            _cancelButton.Disabled = true;

            string host = string.IsNullOrWhiteSpace(status.ServerAddress) ? "127.0.0.1" : status.ServerAddress;
            int port = status.ServerPort ?? 7777;

            NetworkBootstrap.Instance.Gameplay.JoinGameServer(host, port);
            GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Matchmaking error: {ex.Message}");
            _statusLabel.Text = "Matchmaking Failed.";
            ToastNotifications.TryError("Error: Connection failed.", 3.0);
            await Task.Delay(2000);
            Close();
        }
    }

    public async void OnCancelPressed()
    {
        _isClosing = true;
        _statusLabel.Text = "Cancelling...";
        _cancelButton.Disabled = true;

        try
        {
            await NetworkBootstrap.Instance.Menu.CancelMatchmakingAsync();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Cancel error: {ex.Message}");
        }
        finally
        {
            Close();
        }
    }

    private void Close()
    {
        Visible = false;
        _isClosing = true;
        
        if (_contentToHide != null)
        {
            _contentToHide.Visible = true;
        }
    }
}
