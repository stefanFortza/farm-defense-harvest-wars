using System.Collections.Generic;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.Shared.Enums;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;

public partial class UnitFactory : Node, IInitializable<GameplayContext>
{
	// Acest nod trebuie să fie setat ca "Spawn Path" în MultiplayerSpawner-ul din editor

	public bool IsInitialized { get; private set; } = false;
	private readonly Dictionary<UnitType, PackedScene> _scenes = [];
	private Node2D _unitContainer = null!;

	public void Initialize(GameplayContext data)
	{
		if (IsInitialized) return;
		_unitContainer = data.UnitContainer;
		IsInitialized = true;
	}

	public override void _Ready()
	{
		// Load unit scenes (Hardcoded for now, could be dynamic)
		LoadUnitScene(UnitType.Cow, "res://Entities/Units/Defenders/CowUnit.tscn");
		LoadUnitScene(UnitType.Wolf, "res://Entities/Units/Enemies/WolfUnit.tscn");
	}

	private void LoadUnitScene(UnitType type, string path)
	{
		var scene = GD.Load<PackedScene>(path);
		if (scene != null)
		{
			_scenes[type] = scene;
		}
		else
		{
			GD.PrintErr($"Failed to load unit scene: {path}");
		}
	}

	public void Server_SpawnUnit(UnitType type, Vector2I gridPos, GridSystem grid)
	{
		if (!Multiplayer.IsServer()) return;

		// 1. Instanțiem
		var scene = _scenes[type];
		var unit = scene.Instantiate<BaseUnit>();

		// 2. Setăm datele critice
		unit.Position = grid.GetWorldPosition(gridPos);
		// unit.GridPosition = gridPos; // Unitatea știe unde e

		// 3. Adăugăm în container
		// MAGIC: MultiplayerSpawner va detecta asta și va spawna unitatea la toți clienții
		_unitContainer.AddChild(unit);

		// 4. Înregistrăm în grid-ul logic
		grid.RegisterUnit(gridPos, unit);
	}
}