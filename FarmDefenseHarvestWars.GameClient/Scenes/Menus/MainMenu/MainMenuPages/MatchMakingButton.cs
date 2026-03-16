using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class MatchMakingButton : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += OnPressed;

		// For development convenience, we can auto-press this button if we're already authenticated
#if !RELEASE
		OnPressed();
#endif
	}

	private void OnPressed()
	{
		GD.Print("MatchMakingButton was pressed! Starting matchmaking...");
		NetworkBootstrap.Instance.Gameplay.JoinGameServer();

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
