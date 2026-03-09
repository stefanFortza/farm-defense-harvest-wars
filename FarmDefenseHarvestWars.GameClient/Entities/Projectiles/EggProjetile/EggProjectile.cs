using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Entities.Projectiles;
using Godot;
using System;

public partial class EggProjectile : BaseProjectile
{


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();



		// Doar clientul randează animația procedurală
		if (!Multiplayer.IsServer())
		{
			Tween tween = CreateTween().SetLoops(); // Loop infinit
													// Rotește oul cu 360 grade (Tau) în 0.5 secunde
			tween.TweenProperty(Sprite2D, "rotation", Mathf.Tau, 0.5f).AsRelative();
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
