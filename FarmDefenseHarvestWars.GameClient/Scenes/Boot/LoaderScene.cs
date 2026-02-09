using Godot;

public partial class LoaderScene : Node
{
	[Export] public PackedScene AuthScene { get; set; } = null!;
	public override void _Ready()
	{
		// Verificăm argumentele de lansare
		string[] args = OS.GetCmdlineArgs();
		bool isServer = false;

		foreach (var arg in args)
		{
			if (arg == "--server")
			{
				isServer = true;
				break;
			}
		}

		if (isServer)
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
		GetTree().ChangeSceneToPacked(AuthScene);
	}
}