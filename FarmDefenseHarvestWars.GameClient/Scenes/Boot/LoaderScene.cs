using System.Linq;
using Godot;

public partial class LoaderScene : Node
{
	[Export] public PackedScene AuthScene { get; set; } = null!;
	public override void _Ready()
	{
		// Verificăm argumentele de lansare
		string[] args = OS.GetCmdlineArgs();

		if (args.Contains("--server"))
		{
			StartServerMode();
		}
		else
		{
			StartClientMode();
		}
	}

	private void StartServerMode()
	{
		GD.Print(">>> STARTING IN DEDICATED SERVER MODE <<<");

		NetworkBootstrap.Instance.Gameplay.StartDedicatedServer();
	}

	private void StartClientMode()
	{
		GD.Print(">>> STARTING IN CLIENT MODE <<<");
		// Clientul merge la Login
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToPacked, AuthScene);
	}
}