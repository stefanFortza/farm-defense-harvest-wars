using Godot;
using System;

public partial class GameUI : CanvasLayer
{
    [Export] public Control GameOverPanel { get; set; }
    [Export] public Label WinnerLabel { get; set; }

    public override void _Ready()
    {
        if (GameOverPanel != null) GameOverPanel.Visible = false;
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
