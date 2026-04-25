using FarmDefenseHarvestWars.Shared.Enums;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages.Buttons;

public partial class AttackerMatchmakingButton : MatchmakingButton
{
    public override void _Ready()
    {
        PreferredRole = PlayerRole.Attacker;
        ButtonText = "Play as Attacker";
        base._Ready();
    }
}
