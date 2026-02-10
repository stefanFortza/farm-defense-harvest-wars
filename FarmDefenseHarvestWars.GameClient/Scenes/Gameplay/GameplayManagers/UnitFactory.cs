using System.Collections.Generic;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.Shared.Enums;
using Godot;

public partial class UnitFactory : Node
{
	// Acest nod trebuie să fie setat ca "Spawn Path" în MultiplayerSpawner-ul din editor
	[Export] public Node2D UnitContainer { get; private set; } = null!;

	private readonly Dictionary<UnitType, PackedScene> _scenes = [];
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
		UnitContainer.AddChild(unit, true);

		// 4. Înregistrăm în grid-ul logic
		grid.RegisterUnit(gridPos, unit);
	}
}