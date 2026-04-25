using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;

public partial class GameOverUI : CanvasLayer
{
    [Export] private Label _winnerLabel = null!;

    public void SetWinner(string winnerText)
    {
        if (_winnerLabel != null)
        {
            _winnerLabel.Text = $"Game Over\n{winnerText}";
        }
    }

    public void OnBackToMenuPressed()
    {
        GD.Print("[GameOverUI] Back to Menu pressed.");
        // Cleanup networking before leaving
        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer.Close();
        }
        
        GetTree().ChangeSceneToFile("res://Scenes/Menus/MainMenu/MainMenu.tscn");
    }
}
