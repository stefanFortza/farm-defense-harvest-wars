using Godot;
using System;
using System.Linq;
using FarmDefenseHarvestWars.GameClient.Scenes.Authentication.LoginUI;

public partial class AuthUI : Node
{
	[Export] public PackedScene MainMenuScene { get; set; } = null!;
	[Export] public LoginUI LoginPanel { get; set; } = null!;
	[Export] public RegisterUI RegisterPanel { get; set; } = null!;

	public override void _Ready()
	{
		LoginPanel?.LoginSuccess += OnLoginSuccess;
		RegisterPanel?.RegisterSuccess += OnLoginSuccess;

	}

	// Placeholder for actual auth logic
	public void OnLoginSuccess()
	{
		GD.Print("Authentication successful! Transitioning to Main Menu...");
		GetTree().ChangeSceneToPacked(MainMenuScene);
	}
}
