using Godot;
using Refit;
using System;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class MatchMakingButton : Button
{
	private bool _isSearching;
	private GameplayNetwork Gameplay => NetworkBootstrap.Instance.Gameplay;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += OnPressed;
		Gameplay.ClientJoinStateChanged += OnClientJoinStateChanged;
	}

	public override void _ExitTree()
	{
		if (NetworkBootstrap.Instance?.Gameplay != null)
		{
			NetworkBootstrap.Instance.Gameplay.ClientJoinStateChanged -= OnClientJoinStateChanged;
		}
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
		bool waitingForServerStart = false;

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

			Text = "Connecting...";
			Gameplay.JoinGameServer(host, port);
			waitingForServerStart = true;
			return;
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
			if (!waitingForServerStart)
			{
				_isSearching = false;

				if (IsInsideTree())
				{
					Disabled = false;
					Text = "Play";
				}
			}
		}

	}

	private void OnClientJoinStateChanged(bool isConnecting, string message)
	{
		if (!IsInsideTree())
		{
			return;
		}

		if (isConnecting)
		{
			Disabled = true;
			Text = string.IsNullOrWhiteSpace(message) ? "Connecting..." : message;
			return;
		}

		if (!string.IsNullOrWhiteSpace(message))
		{
			GD.PrintErr($"Match join failed: {message}");
		}

		_isSearching = false;
		Disabled = false;
		Text = "Play";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
