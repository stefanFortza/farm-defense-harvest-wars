using Godot;
using System.Threading.Tasks;
using Refit;
using System;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class MatchMakingButton : Button
{
	private bool _isSearching;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private async void OnPressed()
	{
		if (_isSearching)
		{
			return;
		}

		_isSearching = true;
		Disabled = true;
		Text = "Searching...";

		GD.Print("MatchMakingButton was pressed! Starting matchmaking...");

		try
		{
			var status = await NetworkBootstrap.Instance.Menu.StartMatchmakingUntilFoundAsync();

			if (status == null || !status.MatchFound)
			{
				return;
			}

			string host = string.IsNullOrWhiteSpace(status.ServerAddress) ? "127.0.0.1" : status.ServerAddress;
			int port = status.ServerPort ?? 7777;

			NetworkBootstrap.Instance.Gameplay.JoinGameServer(host, port);
			GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
		}
		catch (ApiException ex)
		{
			GD.PrintErr($"Matchmaking failed: {ex.Message}");
		}
		catch (InvalidOperationException ex)
		{
			GD.PrintErr($"Matchmaking state error: {ex.Message}");
		}
		finally
		{
			if (IsInsideTree())
			{
				Disabled = false;
				Text = "Play";
			}

			_isSearching = false;
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
