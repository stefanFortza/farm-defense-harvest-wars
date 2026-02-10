using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;
using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;

public record GameWorldContext(
	GridSystem Grid,
	Node2D UnitContainer
);

public partial class GameWorld : Node2D
{
	private GameplayManager _managers = null!;
	private GridSystem _gridSystem = null!;
	private Node2D _unitContainer = null!;
	public override void _Ready()
	{
		_managers = GetNode<GameplayManager>("GameplayManagers");
		_gridSystem = GetNode<GridSystem>("Map/GridSystem");
		_unitContainer = GetNode<Node2D>("UnitContainer");

		var context = new GameWorldContext(
			Grid: _gridSystem,
			UnitContainer: _unitContainer
		);

		_managers.Initialize(context);
	}
}
