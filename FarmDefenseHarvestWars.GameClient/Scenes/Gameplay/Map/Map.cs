using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;
using System;
using System.Runtime.CompilerServices;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;

public partial class Map : Node2D
{
	[Export] public GridSystem GridSystem { get; private set; } = null!;
	public override void _Ready()
	{
		this.EnsureNotNull(GridSystem, nameof(GridSystem));
	}


}
