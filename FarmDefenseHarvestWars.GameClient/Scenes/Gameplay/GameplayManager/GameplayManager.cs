using FarmDefenseHarvestWars.GameClient.Core.Utils;
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
	Node2D UnitContainer
);


public partial class GameplayManager : Node, IInitializable<GameWorldContext>

{
	[Export] private UnitRegistry UnitRegistry = null!;
	[Export] private MatchManager _matchManager = null!;
	[Export] private UnitFactory _unitFactory = null!;
	[Export] private GameplayOrchestrator _orchestrator = null!;
	[Export] private InputController _inputController = null!;

	public bool IsInitialized { get; private set; } = false;


	public void Initialize(GameWorldContext data)
	{
		if (IsInitialized) return;

		ValidateDependencies();

		var gameplayContext = new GameplayContext(
			Orchestrator: _orchestrator,
			Grid: data.Grid,
			Factory: _unitFactory,
			Match: _matchManager,
			UnitRegistry: UnitRegistry,
			UnitContainer: data.UnitContainer
		);


		_unitFactory.Initialize(gameplayContext);
		_orchestrator.Initialize(gameplayContext);
		_inputController.Initialize(gameplayContext);

		_unitFactory.Server_SpawnUnit(UnitType.Cow, new Vector2I(3, 3), data.Grid);

		IsInitialized = true;
	}

	private void ValidateDependencies()
	{
		this.EnsureNotNull(UnitRegistry, nameof(UnitRegistry));
		this.EnsureNotNull(_matchManager, nameof(_matchManager));
		this.EnsureNotNull(_unitFactory, nameof(_unitFactory));
		this.EnsureNotNull(_orchestrator, nameof(_orchestrator));
		this.EnsureNotNull(_inputController, nameof(_inputController));
	}
}
