using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.Shared.Enums;
using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;

public record GameplayContext(
	GameplayOrchestrator Orchestrator,
	GridSystem Grid,
	UnitFactory Factory,
	MatchManager Match,
	UnitRegistry UnitRegistry,
	Node2D UnitContainer,
	Node2D ProjectileContainer
);


public partial class GameplayManager : Node, IInitializable<GameWorldContext>

{
	[Export] private UnitRegistry _unitRegistry = null!;
	[Export] private MatchManager _matchManager = null!;
	[Export] private UnitFactory _unitFactory = null!;
	[Export] private GameplayOrchestrator _orchestrator = null!;
	[Export] private InputController _inputController = null!;

	public bool IsInitialized { get; private set; } = false;

	public GameHudContext CreateHudContext()
	{
		ValidateDependencies();

		return new GameHudContext(
			Match: _matchManager,
			Input: _inputController,
			UnitRegistry: _unitRegistry
		);
	}


	public void Initialize(GameWorldContext data)
	{
		if (IsInitialized) return;

		ValidateDependencies();

		var gameplayContext = new GameplayContext(
			Orchestrator: _orchestrator,
			Grid: data.Grid,
			Factory: _unitFactory,
			Match: _matchManager,
			UnitRegistry: _unitRegistry,
			UnitContainer: data.UnitContainer,
			ProjectileContainer: data.ProjectileContainer
		);


		_unitFactory.Initialize(gameplayContext);
		_orchestrator.Initialize(gameplayContext);
		_inputController.Initialize(gameplayContext);

		if (Multiplayer.IsServer())
		{
			_unitFactory.Server_SpawnUnit(UnitType.Chicken, new Vector2I(6, 5), data.Grid);
		}

		IsInitialized = true;
	}

	private void ValidateDependencies()
	{
		this.EnsureNotNull(_unitRegistry, nameof(_unitRegistry));
		this.EnsureNotNull(_matchManager, nameof(_matchManager));
		this.EnsureNotNull(_unitFactory, nameof(_unitFactory));
		this.EnsureNotNull(_orchestrator, nameof(_orchestrator));
		this.EnsureNotNull(_inputController, nameof(_inputController));
	}
}
