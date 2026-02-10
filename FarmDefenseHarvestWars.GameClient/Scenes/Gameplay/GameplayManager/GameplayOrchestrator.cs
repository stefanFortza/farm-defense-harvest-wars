using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using Godot;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;

public partial class GameplayOrchestrator : Node, IInitializable<GameplayContext>
{
	private GridSystem _gridSystem = null!;
	private UnitFactory _unitFactory = null!;
	private MatchManager _matchManager = null!;

	public bool IsInitialized { get; private set; } = false;

	public void Initialize(GameplayContext data)
	{
		if (IsInitialized) return;
		_gridSystem = data.Grid;
		_unitFactory = data.Factory;
		_matchManager = data.Match;
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
		long senderId = Multiplayer.GetRemoteSenderId();
		UnitType type = (UnitType)typeInt;

		// 0. Luăm datele din Registry
		var stats = UnitStatsRegistry.Get(type);

		// 1. Validare Logică (Grid)
		if (_gridSystem.IsCellOccupied(gridPos)) return;

		// 2. Validare Economică (MatchManager)
		if (!_matchManager.CanAfford(senderId, stats.Cost))
		{
			GD.Print($"Player {senderId} is broke! Needs {stats.Cost} Gold.");
			return;
		}

		// 3. Execuție
		_matchManager.DeductMoney(senderId, stats.Cost);
		_unitFactory.Server_SpawnUnit(type, gridPos, _gridSystem);
	}
}
