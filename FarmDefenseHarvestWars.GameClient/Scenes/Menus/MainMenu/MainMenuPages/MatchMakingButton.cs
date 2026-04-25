using FarmDefenseHarvestWars.Shared.Enums;
using Godot;
using Refit;
using System;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MainMenuPages;

public partial class MatchMakingButton : Button
{
	[Export] public PlayerRole PreferredRole { get; set; } = PlayerRole.Any;
	[Export] public Control? MatchmakingPanel { get; set; }
	private bool _isSearching;
	private GameplayNetwork Gameplay => NetworkBootstrap.Instance.Gameplay;
	private string _originalText = "";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_originalText = Text;
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
		MatchmakingPanel?.Show();
		bool waitingForServerStart = false;

		GD.Print($"MatchMakingButton was pressed! Starting matchmaking as {PreferredRole}...");

		try
		{
			var status = await NetworkBootstrap.Instance.Menu.StartMatchmakingUntilFoundAsync(PreferredRole);

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
				MatchmakingPanel?.Hide();

				if (IsInsideTree())
				{
					Disabled = false;
					Text = _originalText;
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
		Text = _originalText;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
