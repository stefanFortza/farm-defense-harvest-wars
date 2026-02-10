using Godot;
using System;
using System.Runtime.CompilerServices;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;

public partial class Map : Node2D
{
	public GridSystem GridSystem { get; private set; } = null!;
	public override void _Ready()
	{
		GridSystem = GetNode<GridSystem>("GridSystem");
	}


}
