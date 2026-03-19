using Godot;
using System.Threading.Tasks;
using Refit;

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
			var api = NetworkBootstrap.Instance.ApiClient;
			await api.QueueForMatchAsync();

			while (IsInsideTree())
			{
				var status = await api.GetMatchmakingStatusAsync();
				if (status.MatchFound)
				{
					string host = string.IsNullOrWhiteSpace(status.ServerAddress) ? "127.0.0.1" : status.ServerAddress;
					int port = status.ServerPort ?? 7777;

					NetworkBootstrap.Instance.Gameplay.JoinGameServer(host, port);
					GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
					return;
				}

				await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
			}
		}
		catch (ApiException ex)
		{
			GD.PrintErr($"Matchmaking failed: {ex.Message}");
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
