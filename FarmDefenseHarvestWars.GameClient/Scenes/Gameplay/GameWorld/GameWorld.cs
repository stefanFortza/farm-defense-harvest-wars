using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using Godot;
using System;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;

public record GameWorldContext(
	GridSystem Grid,
	Node2D UnitContainer,
	Node2D ProjectileContainer
);

public record GameHudContext(
	MatchManager Match,
	InputController Input,
	UnitRegistry UnitRegistry
);

public partial class GameWorld : Node2D
{
	[Export] private GameplayManager _managers = null!;
	[Export] private GridSystem _gridSystem = null!;
	[Export] private Node2D _unitContainer = null!;
	[Export] private Node2D _projectileContainer = null!;
	[Export] private GameHUD _gameHUD = null!;

	public override void _Ready()
	{
		this.EnsureNotNull(_managers, nameof(_managers));
		this.EnsureNotNull(_gridSystem, nameof(_gridSystem));
		this.EnsureNotNull(_unitContainer, nameof(_unitContainer));
		this.EnsureNotNull(_projectileContainer, nameof(_projectileContainer));
		this.EnsureNotNull(_gameHUD, nameof(_gameHUD));

		var context = new GameWorldContext(
			Grid: _gridSystem,
			UnitContainer: _unitContainer,
			ProjectileContainer: _projectileContainer
		);

		_managers.Initialize(context);

		var hudContext = _managers.CreateHudContext();
		_gameHUD.Initialize(hudContext);
	}
}
