using Godot;
using System;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;


namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;

public partial class InputController : Node, IInitializable<GameplayContext>
{
	public enum LocalInputState { Idle, PlacingUnit }

	private GridSystem _gridSystem = null!;
	private GameplayOrchestrator _orchestrator = null!;
	private UnitRegistry _unitRegistry = null!;
	[Export] private AnimatedSprite2D _ghostCursor = null!;

	private LocalInputState _currentState = LocalInputState.Idle;
	private UnitType _pendingUnitType = UnitType.None;

	public bool IsInitialized { get; private set; } = false;


	public void Initialize(GameplayContext data)
	{
		if (IsInitialized) return;
		_gridSystem = data.Grid;
		_orchestrator = data.Orchestrator;
		_unitRegistry = data.UnitRegistry;
		IsInitialized = true;
	}

	public override void _Ready()
	{
		_ghostCursor.Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Respinge inputul dacă nu ești într-o stare validă (opțional, poți verifica GameState)

		if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
		{
			if (mouseBtn.ButtonIndex == MouseButton.Left)
			{
				HandleLeftClick();
			}
			else if (mouseBtn.ButtonIndex == MouseButton.Right)
			{
				CancelPlacement(); // Click dreapta anulează tot
			}
		}
	}

	public override void _Process(double delta)
	{
		if (_currentState == LocalInputState.PlacingUnit)
		{
			UpdateGhostPosition();
		}
	}

	// --- STATE MANAGEMENT ---

	// Metoda asta o apelezi din UI (Butonul de "Cumpără Vacă")
	public void StartPlacingUnit(UnitType type)
	{
		var stats = _unitRegistry.GetUnitData(type);

		_currentState = LocalInputState.PlacingUnit;
		_pendingUnitType = type;

		// Configurare Ghost
		if (_ghostCursor != null)
		{
			_ghostCursor.Visible = true;
			_ghostCursor.Play("default");
		}
	}

	private void UpdateGhostPosition()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();

		// 1. Snap la Grid
		Vector2I gridPos = _gridSystem.GetGridPosition(mousePos);
		Vector2 snappedPos = _gridSystem.GetWorldPosition(gridPos);

		if (_ghostCursor != null)
		{
			_ghostCursor.GlobalPosition = snappedPos;

			// 2. Validare vizuală (Modulate pe culoare)
			bool isValid = IsPlacementValid(gridPos);
			_ghostCursor.Play("default");
			_ghostCursor.SelfModulate = isValid ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);
		}
	}

	private void HandleLeftClick()
	{
		switch (_currentState)
		{
			case LocalInputState.Idle:
				// Logică de selecție (Viitor: Select unit/building)
				// Momentan schimbă starea pentru testare rapidă
				StartPlacingUnit(UnitType.Cow);
				break;

			case LocalInputState.PlacingUnit:
				TryPlaceUnit();
				break;
		}
	}

	private void TryPlaceUnit()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector2I gridPos = _gridSystem.GetGridPosition(mousePos);

		if (IsPlacementValid(gridPos))
		{
			// Trimitem cererea către Orchestrator (Server)
			_orchestrator.RequestPlaceUnit(_pendingUnitType, gridPos);

			GD.Print($"InputController: Requesting placement of {_pendingUnitType} at {gridPos}");

			// Dacă vrei să poți pune mai multe la rând, nu reseta starea.
			// Pentru moment resetăm după o plasare reușită.
			CancelPlacement();
		}
	}

	public void CancelPlacement()
	{
		_currentState = LocalInputState.Idle;
		_pendingUnitType = UnitType.None;
		_ghostCursor.Visible = false;
	}

	private bool IsPlacementValid(Vector2I gridPos)
	{
		// 1. Verifică limitele și dacă tile-ul e construibil
		if (!_gridSystem.IsInsideBounds(gridPos) || !_gridSystem.IsTileBuildable(gridPos))
			return false;

		// 2. Verifică dacă tile-ul e liber
		if (_gridSystem.IsCellOccupied(gridPos))
			return false;

		// 3. Verifică economia (Client-side validation for UX)
		// MatchManager-ul are datele despre toți jucătorii pe server, 
		// dar pentru o experiență fluidă, ar fi bine să știm și local.
		// Momentan MatchManager rulează logică doar pe Server.
		// TODO: Sincronizează banii via Rpc/MultiplayerSynchronizer pentru a permite check-ul aici.

		return true;
	}
}