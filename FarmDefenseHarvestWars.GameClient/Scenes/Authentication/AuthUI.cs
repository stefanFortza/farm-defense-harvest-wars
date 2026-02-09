using Godot;
using System;
using System.Linq;

public partial class AuthUI : Node
{
	[Export] public PackedScene MainMenuScene { get; set; } = null!;
	[Export] public LoginUI LoginPanel { get; set; } = null!;
	[Export] public RegisterUI RegisterPanel { get; set; } = null!;

	public override void _Ready()
	{
		// Check for server mode to bypass auth
		if (OS.GetCmdlineArgs().Contains("--server"))
		{
			// Use CallDeferred to avoid "Parent node is busy" error during _Ready
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://Scenes/Menus/MainMenu.tscn");
			return;
		}

		// Connect signals
		LoginPanel?.LoginSuccess += OnLoginSuccess;

		RegisterPanel?.RegisterSuccess += OnLoginSuccess;

	}

	// Placeholder for actual auth logic
	public void OnLoginSuccess()
	{
		GetTree().ChangeSceneToPacked(MainMenuScene);
	}
}
