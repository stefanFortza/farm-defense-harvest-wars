using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MenuLabel;

[Tool]
public partial class MenuLabel : Control
{
    [Export] public Label TitleLabel = null!;
    private string _buttonText = "demo";

    [Export]
    public string ButtonText
    {
        get => _buttonText;
        set
        {

            _buttonText = value;
            ApplyText();
        }
    }

    public override void _Ready()
    {
        this.EnsureNotNull(TitleLabel, nameof(TitleLabel));
        ApplyText();
    }

    private void ApplyText()
    {
        if (TitleLabel == null) return;

        TitleLabel.Text = _buttonText;
    }
}