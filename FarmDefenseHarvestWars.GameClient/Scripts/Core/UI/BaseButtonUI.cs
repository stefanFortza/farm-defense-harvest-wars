using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;
using System;

public partial class BaseButtonUI : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
	}
	public void OnMouseEntered()
	{

		UIAnimations.TryAnimateScaleUp(this, 0.15);
	}

	public void OnMouseExited()
	{
		UIAnimations.TryAnimateScaleDown(this, 0.15);
	}

}
