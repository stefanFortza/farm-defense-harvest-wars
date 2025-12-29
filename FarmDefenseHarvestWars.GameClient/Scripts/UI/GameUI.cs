using Godot;
using System;

public partial class GameUI : CanvasLayer
{
    [Export] public Control GameOverPanel { get; set; } = null!;
    [Export] public Label WinnerLabel { get; set; } = null!;
    [Export] public Label RoleLabel { get; set; } = null!;

    public override void _Ready()
    {
        if (GameOverPanel != null) GameOverPanel.Visible = false;
        UpdateRoleLabel();
    }

    public void UpdateRoleLabel()
    {
        if (RoleLabel != null)
        {
            var role = NetworkManager.Instance.GetCurrentRole();
            RoleLabel.Text = $"Role: {role}";
        }
    }

    public void ShowGameOver(string winnerName)
    {
        if (WinnerLabel != null) WinnerLabel.Text = $"{winnerName} Wins!";
        if (GameOverPanel != null) GameOverPanel.Visible = true;
    }

    public void OnBackToMenuPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Menus/MainMenu.tscn");
    }

    public void OnDebugWinPressed()
    {
        ShowGameOver("Player");
    }
}
