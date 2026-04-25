using FarmDefenseHarvestWars.Shared.Enums;
using Godot;
using FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.Components;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages.Buttons;

public partial class MatchmakingButton : Button
{
    [Export] public PlayerRole PreferredRole { get; set; } = PlayerRole.Any;
    [Export] public MatchmakingOverlay? Overlay { get; set; }

    [Export] public Texture2D? ButtonIcon { get; set; }
    [Export] public string ButtonText { get; set; } = "Matchmaking";

    public override void _Ready()
    {
        Pressed += OnPressed;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        // Initial setup of visuals if nodes are available
        // Note: subclasses can override this or we can use node names
        Text = ButtonText;
    }

    public void OnMouseEntered()
    {
        UIAnimations.TryAnimateScale(this, new Vector2(1.1f, 1.1f), 0.15);
    }

    public void OnMouseExited()
    {
        UIAnimations.TryAnimateScale(this, Vector2.One, 0.15);
    }

    private async void OnPressed()
    {
        if (Overlay != null)
        {
            await Overlay.StartSearch(PreferredRole);
        }
        else
        {
            GD.PrintErr($"{Name}: MatchmakingOverlay not assigned!");
        }
    }
}
