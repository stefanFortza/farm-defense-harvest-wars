using FarmDefenseHarvestWars.GameClient.Entities.Projectiles;
using Godot;

public partial class EggProjectile : BaseProjectile
{
	public override void _Ready()
	{
		base._Ready();

		// Only clients render the decorative spin.
		if (!Multiplayer.IsServer())
		{
			Tween tween = CreateTween().SetLoops();
			tween.TweenProperty(Sprite2D, "rotation", Mathf.Tau, 0.5f).AsRelative();
		}
	}
}
