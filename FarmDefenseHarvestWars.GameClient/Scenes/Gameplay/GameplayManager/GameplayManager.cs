using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
	[Export] public InputController _inputController = null!;

	public MatchManager MatchManager => _matchManager;

	public bool IsInitialized { get; private set; } = false;

	public GameHudContext CreateHudContext()
	{
		ValidateDependencies();

		return new GameHudContext(
			Match: _matchManager,
			Input: _inputController,
			UnitRegistry: _unitRegistry,
			AssignedRole: GameState.Instance?.AssignedRole
		);
	}


	private GridSystem? _grid;
	private bool _initialUnitsSpawned = false;

	public void Initialize(GameWorldContext data)
	{
		if (IsInitialized) return;

		ValidateDependencies();
		_grid = data.Grid;
		_initialUnitsSpawned = false;

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

		// Subscribe to match state changes
		_matchManager.MatchStateChanged += OnMatchStateChanged;

		IsInitialized = true;
	}

	public override void _ExitTree()
	{
		if (_matchManager != null)
		{
			_matchManager.MatchStateChanged -= OnMatchStateChanged;
		}
	}

	private void OnMatchStateChanged(int newStateInt)
	{
		var newState = (MatchManager.MatchState)newStateInt;
		
		if (newState == MatchManager.MatchState.Playing && Multiplayer.IsServer() && !_initialUnitsSpawned)
		{
			if (_grid != null)
			{
				_initialUnitsSpawned = true;
				SpawnInitialUnits(_grid);
			}
			else
			{
				GD.PrintErr("[GameplayManager] Cannot spawn initial units: Grid is null!");
			}
		}
	}

	private void SpawnInitialUnits(GridSystem grid)
	{
		// If match is configured with decks from environment, spawn from those
		if (GameState.Instance.IsMatchConfigured)
		{
			SpawnDeckUnits(grid, GameState.Instance.DefenderDeck!, PlayerRole.Defender);
			SpawnDeckUnits(grid, GameState.Instance.AttackerDeck!, PlayerRole.Attacker);
			GD.Print($"[GameplayManager] Spawned initial units from match decks");
		}
		else
		{
			// Fallback to test unit for development/client testing
			_unitFactory.Server_SpawnUnit(UnitType.Chicken, new Vector2I(6, 5), grid);
			GD.Print($"[GameplayManager] Spawned test Chicken unit (no match configuration)");
		}
	}

	private void SpawnDeckUnits(GridSystem grid, IReadOnlyList<UnitUnlockDto> deck, PlayerRole role)
	{
		if (deck == null || deck.Count == 0)
		{
			GD.PrintErr($"[GameplayManager] Attempted to spawn empty {role} deck");
			return;
		}

		// Defender spawns on the left side (starting column 6)
		// Attacker spawns on the right side (ending around column 19)
		int startX = role == PlayerRole.Defender ? 6 : 16;
		int rowOffset = 0;
		int maxRowsPerColumn = 5; // To distribute units across the 5 lanes (Y: 3-7)

		GD.Print($"[GameplayManager] Spawning {role} deck: {string.Join(", ", deck.Select(u => u.UnitType))}");

		foreach (var unlock in deck)
		{
			// Calculate grid position
			int x = startX + (rowOffset / maxRowsPerColumn);
			int y = 3 + (rowOffset % maxRowsPerColumn);

			// Clamp to valid spawn area
			if (role == PlayerRole.Defender && x > 15) x = 15;
			if (role == PlayerRole.Attacker && x > 19) x = 19;

			Vector2I spawnPos = new Vector2I(x, y);

			try
			{
				_unitFactory.Server_SpawnUnit(unlock.UnitType, spawnPos, grid, unlock.Level);
				GD.Print($"[GameplayManager] Spawned {unlock.UnitType} (Lvl {unlock.Level}) at {spawnPos} for {role}");
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[GameplayManager] Failed to spawn {unlock.UnitType} at {spawnPos}: {ex.Message}");
			}

			rowOffset++;
		}
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
