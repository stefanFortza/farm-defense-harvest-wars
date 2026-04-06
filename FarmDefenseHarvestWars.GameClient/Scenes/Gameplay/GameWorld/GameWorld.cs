using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.Shared.Enums;
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
	UnitRegistry UnitRegistry,
	PlayerRole? AssignedRole
);

public partial class GameWorld : Node2D
{
	[Export] private GameplayManager _managers = null!;
	[Export] private GridSystem _gridSystem = null!;
	[Export] private Node2D _unitContainer = null!;
	[Export] private Node2D _projectileContainer = null!;
	[Export] private GameHUD _gameHUD = null!;

	[ExportGroup("Multiplayer Spawners")]
	[Export] private MultiplayerSpawner _unitSpawner = null!;
	[Export] private MultiplayerSpawner _projectileSpawner = null!;

	public override void _Ready()
	{
		this.EnsureNotNull(_managers, nameof(_managers));
		this.EnsureNotNull(_gridSystem, nameof(_gridSystem));
		this.EnsureNotNull(_unitContainer, nameof(_unitContainer));
		this.EnsureNotNull(_projectileContainer, nameof(_projectileContainer));
		this.EnsureNotNull(_gameHUD, nameof(_gameHUD));
		this.EnsureNotNull(_unitSpawner, nameof(_unitSpawner));
		this.EnsureNotNull(_projectileSpawner, nameof(_projectileSpawner));

		AutoRegisterSpawnableScenes();

		var context = new GameWorldContext(
			Grid: _gridSystem,
			UnitContainer: _unitContainer,
			ProjectileContainer: _projectileContainer
		);

		_managers.Initialize(context);

		var hudContext = _managers.CreateHudContext();
		_gameHUD.Initialize(hudContext);
	}

	private void AutoRegisterSpawnableScenes()
	{
		// 1. Scan Units (Defenders & Enemies)
		string[] unitFolders = { "res://Entities/Units/Defenders", "res://Entities/Units/Enemies" };
		var allUnitScenes = new System.Collections.Generic.List<string>();

		foreach (var folder in unitFolders)
		{
			ScanFolderForScenes(folder, allUnitScenes);
		}

		// Sort to ensure identical order on client and server
		allUnitScenes.Sort();

		foreach (var scenePath in allUnitScenes)
		{
			_unitSpawner.AddSpawnableScene(scenePath);
		}

		GD.Print($"[GameWorld] Auto-registered {allUnitScenes.Count} unit scenes in UnitSpawner.");

		// 2. Scan Projectiles
		string projectileFolder = "res://Entities/Projectiles";
		var allProjectileScenes = new System.Collections.Generic.List<string>();
		ScanFolderForScenes(projectileFolder, allProjectileScenes);
		allProjectileScenes.Sort();

		foreach (var scenePath in allProjectileScenes)
		{
			_projectileSpawner.AddSpawnableScene(scenePath);
		}

		GD.Print($"[GameWorld] Auto-registered {allProjectileScenes.Count} projectile scenes in ProjectileSpawner.");
	}

	private void ScanFolderForScenes(string path, System.Collections.Generic.List<string> resultList)
	{
		using var dir = DirAccess.Open(path);
		if (dir == null) return;

		dir.ListDirBegin();
		string fileName = dir.GetNext();

		while (fileName != "")
		{
			if (dir.CurrentIsDir())
			{
				if (!fileName.StartsWith("."))
				{
					ScanFolderForScenes(path.PathJoin(fileName), resultList);
				}
			}
			else if (fileName.EndsWith(".tscn"))
			{
				resultList.Add(path.PathJoin(fileName));
			}
			fileName = dir.GetNext();
		}
	}
}
