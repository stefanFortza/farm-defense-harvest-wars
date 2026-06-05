using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.Shared.Enums;
using Godot;
using System;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Entities.Units.Base;
using FarmDefenseHarvestWars.GameClient.Entities.Projectiles;

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
	[Export] private Node2D _unitContainer = null!;
	[Export] private Node2D _projectileContainer = null!;
	[Export] private Map _map = null!;
	[Export] private DefenderBase _defenderBase = null!;

	[ExportGroup("UI")]
	[Export] private GameHUD _gameHUD = null!;
	[Export] private PackedScene _gameOverScene = null!;

	[ExportGroup("Multiplayer Spawners")]
	[Export] private MultiplayerSpawner _unitSpawner = null!;
	[Export] private MultiplayerSpawner _projectileSpawner = null!;

	private GridSystem _gridSystem = null!;

	public override void _Ready()
	{
		this.EnsureNotNull(_managers, nameof(_managers));
		this.EnsureNotNull(_unitContainer, nameof(_unitContainer));
		this.EnsureNotNull(_projectileContainer, nameof(_projectileContainer));
		this.EnsureNotNull(_gameHUD, nameof(_gameHUD));
		this.EnsureNotNull(_map, nameof(_map));
		this.EnsureNotNull(_unitSpawner, nameof(_unitSpawner));
		this.EnsureNotNull(_projectileSpawner, nameof(_projectileSpawner));
		this.EnsureNotNull(_defenderBase, nameof(_defenderBase));

		this.EnsureNotNull(_map.GridSystem, "Map.GridSystem");
		this.EnsureNotNull(_gameOverScene, nameof(_gameOverScene));

		// Register scenes BEFORE other initialization
		RegisterSpawnableScenesFromRegistry();

		_gridSystem = _map.GridSystem;

		var context = new GameWorldContext(
			Grid: _gridSystem,
			UnitContainer: _unitContainer,
			ProjectileContainer: _projectileContainer
		);

		_managers.Initialize(context);

		// Initialize DefenderBase with the centralized HealthComponent from MatchManager
		_defenderBase.Initialize(_managers.MatchManager.BaseHealthComponent);

		var hudContext = _managers.CreateHudContext();
		_gameHUD.Initialize(hudContext);

		_managers.MatchManager.MatchEnded += OnMatchEnded;

		if (Multiplayer.IsServer())
		{
			Multiplayer.PeerDisconnected += OnPeerDisconnected;
		}

		// Play Gameplay Music
		AudioController.Instance?.PlayGameplayMusic();

		// Handle reconnection: Request world state immediately if we are a client
		if (!Multiplayer.IsServer())
		{
			GD.Print("[GameWorld] Client detected. Checking for immediate sync...");
			RpcId(1, nameof(RequestWorldState));
		}
	}

	public override void _Process(double delta)
	{
		// Guard against null peer after match ends or disconnection
		if (Multiplayer.MultiplayerPeer == null) 
		{
			SetProcess(false);
			return;
		}

		// On server, periodically broadcast world state updates to reconnected players
		try 
		{
			if (Multiplayer.IsServer() && _reconnectedPeers.Count > 0)
			{
				_manualSyncTimer += (float)delta;
				if (_manualSyncTimer >= 0.1f) // 10Hz update
				{
					_manualSyncTimer = 0;
					BroadcastManualSync();
				}
			}
		}
		catch (InvalidOperationException)
		{
			SetProcess(false);
		}
	}

	private float _manualSyncTimer = 0f;
	private readonly System.Collections.Generic.HashSet<long> _reconnectedPeers = new();

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestWorldState()
	{
		if (Multiplayer.MultiplayerPeer == null) return;
		
		try 
		{
			if (!Multiplayer.IsServer()) return;
		}
		catch (InvalidOperationException) { return; }

		long peerId = Multiplayer.GetRemoteSenderId();
		GD.Print($"[GameWorld] Peer {peerId} requested world state sync. Collecting data...");
		_reconnectedPeers.Add(peerId);

		// Collect Units
		var unitData = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var node in _unitContainer.GetChildren())
		{
			if (node is BaseUnit unit)
			{
				var dict = new Godot.Collections.Dictionary
				{
					{ "name", unit.Name },
					{ "scene", unit.SceneFilePath },
					{ "pos", unit.Position },
					{ "hp", unit.CurrentHealth },
					{ "max_hp", unit.MaxHealth },
					{ "level", unit.Level },
					{ "facing", unit.FacingDirection },
					{ "laneY", unit.LaneCenterY },
					{ "state", (int)unit.StateMachine.CurrentState }
				};
				unitData.Add(dict);
			}
		}

		// Collect Projectiles
		var projData = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var node in _projectileContainer.GetChildren())
		{
			if (node is BaseProjectile proj)
			{
				var dict = new Godot.Collections.Dictionary
				{
					{ "name", proj.Name },
					{ "scene", proj.SceneFilePath },
					{ "pos", proj.Position },
					{ "dir", proj.Direction },
					{ "dmg", proj.Damage },
					{ "atk", proj.IsFromAttacker },
					{ "speed", proj.Speed }
				};
				projData.Add(dict);
			}
		}

		RpcId(peerId, nameof(SyncWorldState), unitData, projData);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SyncWorldState(Godot.Collections.Array<Godot.Collections.Dictionary> units, Godot.Collections.Array<Godot.Collections.Dictionary> projectiles)
	{
		if (Multiplayer.MultiplayerPeer == null) return;
		
		GD.Print($"[GameWorld] Received world state: {units.Count} units, {projectiles.Count} projectiles.");

		// Spawn Units
		foreach (var data in units)
		{
			string name = (string)data["name"];
			if (_unitContainer.HasNode(name)) continue;

			string scenePath = (string)data["scene"];
			if (string.IsNullOrEmpty(scenePath)) continue;

			var scene = GD.Load<PackedScene>(scenePath);
			var unit = scene.Instantiate<BaseUnit>();

			unit.Name = name; 
			unit.Position = (Vector2)data["pos"];
			unit.SetLevel((int)data["level"]);
			unit.FacingDirection = (int)data["facing"];
			unit.LaneCenterY = (float)data["laneY"];
			unit.ProjectileContainer = _projectileContainer;

			unit.SetMultiplayerAuthority(1);
			
			var sync = unit.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
			if (sync != null)
			{
				sync.SetProcess(false);
				sync.SetPhysicsProcess(false);
				sync.PublicVisibility = false;
			}

			_unitContainer.AddChild(unit, true);
			
			unit.HealthComponent.SetHealthSilently((int)data["hp"], (int)data["max_hp"]);
			unit.StateMachine.SyncedStateIndex = (int)data["state"];
			
			GD.Print($"[GameWorld] Manually restored unit {name}");
		}

		// Spawn Projectiles
		foreach (var data in projectiles)
		{
			string name = (string)data["name"];
			if (_projectileContainer.HasNode(name)) continue;

			string scenePath = (string)data["scene"];
			if (string.IsNullOrEmpty(scenePath)) continue;

			var scene = GD.Load<PackedScene>(scenePath);
			var proj = scene.Instantiate<BaseProjectile>();

			proj.Name = name;
			proj.Position = (Vector2)data["pos"];
			proj.Initialize(((int)data["dmg"], (Vector2)data["dir"], (bool)data["atk"]));
			proj.Speed = (float)data["speed"];
			
			proj.SetMultiplayerAuthority(1);
			var sync = proj.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
			if (sync != null)
			{
				sync.SetProcess(false);
				sync.SetPhysicsProcess(false);
				sync.PublicVisibility = false;
			}

			_projectileContainer.AddChild(proj, true);
			GD.Print($"[GameWorld] Manually restored projectile {name}");
		}
	}

	private void BroadcastManualSync()
	{
		if (Multiplayer.MultiplayerPeer == null) return;

		var unitUpdates = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var node in _unitContainer.GetChildren())
		{
			if (node is BaseUnit unit)
			{
				unitUpdates.Add(new Godot.Collections.Dictionary {
					{ "n", unit.Name },
					{ "p", unit.Position },
					{ "h", unit.CurrentHealth },
					{ "s", (int)unit.StateMachine.CurrentState }
				});
			}
		}

		var projUpdates = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var node in _projectileContainer.GetChildren())
		{
			if (node is BaseProjectile proj)
			{
				projUpdates.Add(new Godot.Collections.Dictionary {
					{ "n", proj.Name },
					{ "p", proj.Position }
				});
			}
		}
		
		foreach (var peerId in _reconnectedPeers)
		{
			try 
			{
				RpcId(peerId, nameof(UpdateManualWorldState), unitUpdates, projUpdates);
			}
			catch (InvalidOperationException) { }
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void UpdateManualWorldState(Godot.Collections.Array<Godot.Collections.Dictionary> unitUpdates, Godot.Collections.Array<Godot.Collections.Dictionary> projUpdates)
	{
		if (Multiplayer.MultiplayerPeer == null) return;

		// 1. Sync & Track Units
		var aliveUnits = new System.Collections.Generic.HashSet<string>();
		foreach (var data in unitUpdates)
		{
			string name = (string)data["n"];
			aliveUnits.Add(name);
			var unit = _unitContainer.GetNodeOrNull<BaseUnit>(name);
			if (unit != null)
			{
				unit.Position = (Vector2)data["p"];
				unit.HealthComponent.SetHealthSilently((int)data["h"], unit.MaxHealth);
				unit.StateMachine.SyncedStateIndex = (int)data["s"];
			}
		}

		// 2. Sync & Track Projectiles
		var aliveProjectiles = new System.Collections.Generic.HashSet<string>();
		foreach (var data in projUpdates)
		{
			string name = (string)data["n"];
			aliveProjectiles.Add(name);
			var proj = _projectileContainer.GetNodeOrNull<BaseProjectile>(name);
			if (proj != null)
			{
				proj.Position = (Vector2)data["p"];
			}
		}

		// 3. Active Cleanup: Remove nodes that exist locally but are NOT in the server's update
		// This is a safety net for reconnected clients where MultiplayerSpawner might fail to despawn.
		foreach (var node in _unitContainer.GetChildren())
		{
			if (node is BaseUnit unit && !aliveUnits.Contains(unit.Name))
			{
				GD.Print($"[GameWorld] Manual cleanup: Removing stale unit {unit.Name}");
				unit.QueueFree();
			}
		}

		foreach (var node in _projectileContainer.GetChildren())
		{
			if (node is BaseProjectile proj && !aliveProjectiles.Contains(proj.Name))
			{
				GD.Print($"[GameWorld] Manual cleanup: Removing stale projectile {proj.Name}");
				proj.QueueFree();
			}
		}
	}

	public override void _ExitTree()
	{
		if (Multiplayer.IsServer())
		{
			Multiplayer.PeerDisconnected -= OnPeerDisconnected;
		}

		if (_managers?.MatchManager != null)
		{
			_managers.MatchManager.MatchEnded -= OnMatchEnded;
		}

		// Switch back to Menu Music when leaving the world
		AudioController.Instance?.PlayMenuMusic();
	}

	private void OnPeerDisconnected(long id)
	{
		_reconnectedPeers.Remove(id);
	}

	private void OnMatchEnded(int winnerRole)
	{
		GD.Print($"[GameWorld] OnMatchEnded triggered. Winner: {(PlayerRole)winnerRole}");

		SetProcess(false);

		var inputController = _managers._inputController;
		if (inputController != null)
		{
			inputController.CancelPlacement();
			inputController.SetProcess(false);
			inputController.SetProcessInput(false);
			inputController.SetProcessUnhandledInput(false);
		}

		if (_gameOverScene != null)
		{
			var gameOverUI = _gameOverScene.Instantiate<GameOverUI>();
			AddChild(gameOverUI);

			string winnerText = (PlayerRole)winnerRole == PlayerRole.Defender ? "Defender Wins!" : "Attacker Wins!";
			gameOverUI.SetWinner(winnerText);
		}
	}

	private void RegisterSpawnableScenesFromRegistry()
	{
		var registry = _managers.UnitRegistry;
		if (registry == null) return;

		int unitCount = 0;
		foreach (var unit in registry.AllUnits)
		{
			if (unit != null && !string.IsNullOrEmpty(unit.UnitScenePath))
			{
				_unitSpawner.AddSpawnableScene(unit.UnitScenePath);
				unitCount++;
			}
		}

		GD.Print($"[GameWorld] Registered {unitCount} unit scenes from Registry in UnitSpawner.");

		// Register projectiles from Registry and from UnitData
		var projPaths = new System.Collections.Generic.HashSet<string>();

		// 1. Explicit projectiles list in registry
		foreach (var proj in registry.Projectiles)
		{
			if (proj != null && !string.IsNullOrEmpty(proj.ResourcePath))
			{
				projPaths.Add(proj.ResourcePath);
			}
		}

		// 2. Projectiles attached to units
		foreach (var unit in registry.AllUnits)
		{
			if (unit?.ProjectileScene != null && !string.IsNullOrEmpty(unit.ProjectileScene.ResourcePath))
			{
				projPaths.Add(unit.ProjectileScene.ResourcePath);
			}
		}

		int projCount = 0;
		foreach (var path in projPaths)
		{
			_projectileSpawner.AddSpawnableScene(path);
			projCount++;
		}

		GD.Print($"[GameWorld] Registered {projCount} unique projectile scenes in ProjectileSpawner.");
		}
		}