using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using Godot;
using System;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.Map;

public partial class GameplayOrchestrator : Node
{
	[Export] public UnitFactory UnitFactory;
	[Export] public GridSystem GridSystem;
	[Export] public MatchManager MatchManager;

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
		if (GridSystem.IsCellOccupied(gridPos)) return;

		// 2. Validare Economică (MatchManager)
		if (!MatchManager.CanAfford(senderId, stats.Cost))
		{
			GD.Print($"Player {senderId} is broke! Needs {stats.Cost} Gold.");
			return;
		}

		// 3. Execuție
		MatchManager.DeductMoney(senderId, stats.Cost);
		UnitFactory.Server_SpawnUnit(type, gridPos, GridSystem);
	}
}
