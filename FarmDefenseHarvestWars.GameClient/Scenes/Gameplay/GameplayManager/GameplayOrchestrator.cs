using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using Godot;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using System.Linq;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;

public partial class GameplayOrchestrator : Node, IInitializable<GameplayContext>

{
	public bool IsInitialized { get; private set; } = false;
	private long CurrentSender => Multiplayer.GetRemoteSenderId();
	private long _nextRequestId = 1;

	[Signal] public delegate void PlacementRequestResolvedEventHandler(long requestId, int unitType, bool success, string reason, Vector2I gridPos);

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
	public long RequestPlaceUnit(UnitType type, Vector2I gridPos)
	{
		long requestId = _nextRequestId++;
		RpcId(1, nameof(HandlePlaceRequest), requestId, (int)type, gridPos);
		return requestId;
	}

	// --- SERVER SIDE ---
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void HandlePlaceRequest(long requestId, int typeInt, Vector2I gridPos)
	{
		UnitType type = (UnitType)typeInt;

		// 0. Luăm datele din Registry
		var stats = _unitRegistry.GetUnitData(type);

		// 1. Validare Logică (Grid)
		if (_gridSystem.IsCellOccupied(gridPos))
		{
			ResolvePlacementForSender(requestId, type, false, "CELL_OCCUPIED", gridPos);
			return;
		}

		// 2. Validare Economică (MatchManager)
		if (!_matchManager.TryBuyUnit(CurrentSender, stats))
		{
			ResolvePlacementForSender(requestId, type, false, "INSUFFICIENT_GOLD", gridPos);
			return;
		}

		// 3. Execuție
		int unitLevel = 1;

		// Find level from the match deck of the player who requested it
		var network = NetworkBootstrap.Instance?.Gameplay;
		if (network != null && network.TryGetRoleForPeer(CurrentSender, out var role))
		{
			var deck = GameState.Instance.GetMatchDeckForRole(role);
			var unlock = deck.FirstOrDefault(u => u.UnitType == type);
			if (unlock != null)
			{
				unitLevel = unlock.Level;
			}
		}

		_unitFactory.Server_SpawnUnit(type, gridPos, _gridSystem, unitLevel);
		ResolvePlacementForSender(requestId, type, true, "OK", gridPos);
	}

	private void ResolvePlacementForSender(long requestId, UnitType type, bool success, string reason, Vector2I gridPos)
	{
		RpcId(CurrentSender, nameof(ClientResolvePlacementRequest), requestId, (int)type, success, reason, gridPos);

		if (CurrentSender == Multiplayer.GetUniqueId())
		{
			ClientResolvePlacementRequest(requestId, (int)type, success, reason, gridPos);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ClientResolvePlacementRequest(long requestId, int unitType, bool success, string reason, Vector2I gridPos)
	{
		EmitSignal(SignalName.PlacementRequestResolved, requestId, unitType, success, reason, gridPos);
	}
}
