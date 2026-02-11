using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using Godot;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;

public partial class GameplayOrchestrator : Node, IInitializable<GameplayContext>

{
	public bool IsInitialized { get; private set; } = false;
	private long CurrentSender => Multiplayer.GetRemoteSenderId();

	private GridSystem _gridSystem = null!;
	private UnitFactory _unitFactory = null!;
	private MatchManager _matchManager = null!;
	private UnitRegistry _unitRegistry = null!;

	public void Initialize(GameplayContext data)
	{
		if (IsInitialized) return;
		_gridSystem = data.Grid;
		_unitFactory = data.Factory;
		_matchManager = data.Match;
		_unitRegistry = data.UnitRegistry;
		IsInitialized = true;
	}

	// --- CLIENT SIDE ---
	// Apelat din InputController când dai click
	public void RequestPlaceUnit(UnitType type, Vector2I gridPos)
	{
		RpcId(1, nameof(HandlePlaceRequest), (int)type, gridPos);
	}

	// --- SERVER SIDE ---
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void HandlePlaceRequest(int typeInt, Vector2I gridPos)
	{
		UnitType type = (UnitType)typeInt;

		// 0. Luăm datele din Registry
		var stats = _unitRegistry.GetUnitData(type);

		// 1. Validare Logică (Grid)
		if (_gridSystem.IsCellOccupied(gridPos)) return;

		// 2. Validare Economică (MatchManager)
		if (!_matchManager.TryBuyUnit(CurrentSender, stats))
		{
			GD.Print($"Player {CurrentSender} is broke! Needs {stats.MatchCost} Gold.");
			return;
		}

		// 3. Execuție
		_unitFactory.Server_SpawnUnit(type, gridPos, _gridSystem);
	}
}
