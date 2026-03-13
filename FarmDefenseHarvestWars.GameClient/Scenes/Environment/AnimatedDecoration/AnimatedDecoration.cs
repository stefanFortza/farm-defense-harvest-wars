using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Environment;

public partial class AnimatedDecoration : Node2D
{
	[Export] public AnimatedSprite2D AnimSprite { get; set; } = null!;

	[Export] public bool RandomizeStartFrame { get; set; } = true;

	public override void _Ready()
	{
		this.EnsureNotNull(AnimSprite, nameof(AnimSprite));

		AnimSprite.Play();

		if (RandomizeStartFrame && AnimSprite.SpriteFrames != null)
		{
			int frameCount = AnimSprite.SpriteFrames.GetFrameCount(AnimSprite.Animation);
			if (frameCount > 0)
			{
				AnimSprite.Frame = (int)(GD.Randi() % frameCount);

				AnimSprite.SpeedScale = (float)GD.RandRange(0.9, 1.1);
			}
		}
	}
}