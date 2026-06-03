using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;

public partial class SettingsPopup : Control
{
    [Export] private Button _closeButton = null!;

    public override void _Ready()
    {
        if (_closeButton != null)
        {
            _closeButton.Pressed += OnClosePressed;
        }
    }

    private void OnClosePressed()
    {
        AudioController.Instance?.PlaySfx("res://Assets/Audio/ui/click1.ogg");
        QueueFree();
    }
}
