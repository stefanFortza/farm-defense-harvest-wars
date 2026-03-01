using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.Shared.Enums;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;

public partial class UnitFactory : Node, IInitializable<GameplayContext>
{
	public bool IsInitialized { get; private set; } = false;
	private Node2D _unitContainer = null!;
	private Node2D _projectileContainer = null!;
	private UnitRegistry _unitRegistry = null!;

	public void Initialize(GameplayContext data)
	{
		if (IsInitialized) return;
		_unitContainer = data.UnitContainer;
		_projectileContainer = data.ProjectileContainer;
		_unitRegistry = data.UnitRegistry;
		IsInitialized = true;
	}

	public void Server_SpawnUnit(UnitType type, Vector2I gridPos, GridSystem grid)
	{
		if (!Multiplayer.IsServer()) return;

		// 1. Luăm datele din Registry
		var unitData = _unitRegistry.GetUnitData(type);
		if (string.IsNullOrEmpty(unitData.UnitScenePath))
		{
			GD.PrintErr($"[UnitFactory] UnitScenePath is empty for unit type: {type}");
			return;
		}

		// 2. Instanțiem
		var scene = GD.Load<PackedScene>(unitData.UnitScenePath);
		if (scene == null)
		{
			GD.PrintErr($"[UnitFactory] Failed to load scene at: {unitData.UnitScenePath}");
			return;
		}

		var unit = scene.Instantiate<BaseUnit>();
		unit.ProjectileContainer = _projectileContainer;

		// 3. Setăm datele critice
		unit.Position = grid.GetWorldPosition(gridPos);
		// unit.GridPosition = gridPos; // Unitatea știe unde e

		// 4. Adăugăm în container
		// MAGIC: MultiplayerSpawner va detecta asta și va spawna unitatea la toți clienții
		_unitContainer.AddChild(unit, true);

		bool occupiesGrid = unitData.Speed <= 0f;

		// 5. Înregistrăm în grid-ul logic
		if (occupiesGrid)
		{
			grid.RegisterUnit(gridPos, unit);
			GD.Print($"[UnitFactory] Static unit {type} registered at grid {gridPos}.");
		}
		else
		{
			GD.Print($"[UnitFactory] Dynamic unit {type} spawned at {gridPos}. Grid occupancy bypassed.");
		}
	}
}