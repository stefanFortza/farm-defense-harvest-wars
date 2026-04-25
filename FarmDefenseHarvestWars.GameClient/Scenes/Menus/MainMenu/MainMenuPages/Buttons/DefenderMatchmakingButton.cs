using FarmDefenseHarvestWars.Shared.Enums;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages.Buttons;

public partial class DefenderMatchmakingButton : MatchmakingButton
{
    public override void _Ready()
    {
        PreferredRole = PlayerRole.Defender;
        ButtonText = "Play as Defender";
        base._Ready();
    }
}
