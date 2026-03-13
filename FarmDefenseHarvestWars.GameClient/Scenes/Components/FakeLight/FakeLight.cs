using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Environment;

public partial class FakeLight : Sprite2D
{
	[ExportCategory("Light Settings")]
	[Export] public Color BaseColor { get; set; } = new Color(1.0f, 1.0f, 0.8f, 0.2f); // Galben pal, foarte transparent

	// În jocurile top-down/izometrice, baza e de obicei un oval, nu un cerc perfect.
	[Export(PropertyHint.Range, "0.1, 5.0, 0.1")]
	public float RadiusMultiplier { get; set; } = 1.0f;

	[Export] public bool IsOval = true;

	public override void _Ready()
	{
		// Aplicăm culoarea și transparența setate în editor
		Modulate = BaseColor;

		// Deformăm textura pentru a crea o perspectivă corectă pe sol
		if (IsOval)
		{
			Scale = new Vector2(RadiusMultiplier, RadiusMultiplier * 0.5f);
		}
		else
		{
			Scale = new Vector2(RadiusMultiplier, RadiusMultiplier);
		}
	}
}