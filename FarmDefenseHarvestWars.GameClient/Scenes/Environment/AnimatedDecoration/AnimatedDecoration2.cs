using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Environment;

public partial class AnimatedDecoration2 : AnimatedSprite2D
{

	[Export] public bool RandomizeStartFrame { get; set; } = true;

	public override void _Ready()
	{

		this.Play();

		if (RandomizeStartFrame && SpriteFrames != null)
		{
			int frameCount = SpriteFrames.GetFrameCount(Animation);
			if (frameCount > 0)
			{
				Frame = (int)(GD.Randi() % frameCount);

				SpeedScale = (float)GD.RandRange(0.9, 1.1);
			}
		}
	}
}