using FarmDefenseHarvestWars.Shared.Enums;
using Godot;
using FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.Components;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class MatchMakingButton : Button
{
	[Export] public PlayerRole PreferredRole { get; set; } = PlayerRole.Any;
	[Export] public MatchmakingOverlay? Overlay { get; set; }

	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private async void OnPressed()
	{
		if (Overlay != null)
		{
			await Overlay.StartSearch(PreferredRole);
		}
		else
		{
			GD.PrintErr("MatchMakingButton: Overlay not assigned!");
		}
	}
}
