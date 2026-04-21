using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MenuLabel;

[Tool]
public partial class MenuLabel : Control
{
    [Export] private Label _label = null!;
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
        this.EnsureNotNull(_label, nameof(_label));
        ApplyText();
    }

    private void ApplyText()
    {
        _label.Text = _buttonText;
    }
}