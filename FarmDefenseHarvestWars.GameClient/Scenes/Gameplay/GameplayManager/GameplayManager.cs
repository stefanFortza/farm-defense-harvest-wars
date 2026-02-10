using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;

public record GameplayContext(
	GameplayOrchestrator Orchestrator,
	GridSystem Grid,
	UnitFactory Factory,
	MatchManager Match,
	Node2D UnitContainer
);


public partial class GameplayManager : Node, IInitializable<GameWorldContext>

{
	private MatchManager _matchManager = null!;
	private UnitFactory _unitFactory = null!;
	private GameplayOrchestrator _orchestrator = null!;
	private InputController _inputController = null!;

	public bool IsInitialized { get; private set; } = false;

	public void Initialize(GameWorldContext data)
	{
		if (IsInitialized) return;

		_matchManager = GetNode<MatchManager>("MatchManager");
		_unitFactory = GetNode<UnitFactory>("UnitFactory");
		_orchestrator = GetNode<GameplayOrchestrator>("GameplayOrchestrator");
		_inputController = GetNode<InputController>("InputController");

		var gameplayContext = new GameplayContext(
			Orchestrator: _orchestrator,
			Grid: data.Grid,
			Factory: _unitFactory,
			Match: _matchManager,
			UnitContainer: data.UnitContainer
		);


		_unitFactory.Initialize(gameplayContext);
		_orchestrator.Initialize(gameplayContext);
		_inputController.Initialize(gameplayContext);

		IsInitialized = true;
	}
}
