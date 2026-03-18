using Godot;
using System;
using System.Collections.Generic;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

public partial class MatchManager : Node
{
    public enum MatchState { Waiting, Playing, Ended }

    [Export] public float MatchDurationSeconds = 300f; // 5 minutes
    [Export] public int StartingMoney = 100;
    [Export] public int PassiveIncomeAmount = 5;
    [Export] public float PassiveIncomeInterval = 1.0f;
    [Export] public int MaxBaseHealth = 100;
    [Export] public float StateSyncInterval = 0.2f;

    private MatchState _currentState = MatchState.Waiting;
    private float _timeRemaining;
    private int _baseHealth;
    private float _incomeTimer = 0f;
    private float _stateSyncTimer = 0f;

    // PeerID -> Money
    private readonly Dictionary<long, int> _playerMoney = new();

    [Signal] public delegate void MatchStateChangedEventHandler(int newState);
    [Signal] public delegate void MoneyChangedEventHandler(long peerId, int newAmount);
    [Signal] public delegate void BaseHealthChangedEventHandler(int current, int max);
    [Signal] public delegate void MatchEndedEventHandler(int winnerRole);
    [Signal] public delegate void TimerUpdatedEventHandler(float timeRemaining);

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            ResetMatch();
            return;
        }

        SetProcess(false);
        CallDeferred(nameof(RequestFullSync));
    }

    public override void _ExitTree()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        Multiplayer.PeerConnected -= OnPeerConnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;
    }

    public void ResetMatch()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        _currentState = MatchState.Playing;
        _timeRemaining = MatchDurationSeconds;
        _baseHealth = MaxBaseHealth;
        _playerMoney.Clear();
        _incomeTimer = 0f;
        _stateSyncTimer = 0f;

        // Inițializăm banii pentru jucătorii deja conectați
        foreach (var id in Multiplayer.GetPeers())
        {
            _playerMoney[id] = StartingMoney;
            EmitSignal(SignalName.MoneyChanged, id, StartingMoney);
        }

        // Serverul poate fi și el un jucător (Peer 1)
        _playerMoney[1] = StartingMoney;
        EmitSignal(SignalName.MoneyChanged, 1L, StartingMoney);

        EmitSignal(SignalName.BaseHealthChanged, _baseHealth, MaxBaseHealth);
        EmitSignal(SignalName.MatchStateChanged, (int)_currentState);
        EmitSignal(SignalName.TimerUpdated, _timeRemaining);

        BroadcastSnapshot();
        BroadcastAllMoney();

        Logger.Info("MatchManager: Match Reset and Started.");
    }

    public override void _Process(double delta)
    {
        if (!Multiplayer.IsServer() || _currentState != MatchState.Playing) return;

        float dt = (float)delta;
        UpdateTimer(dt);
        UpdateEconomy(dt);

        _stateSyncTimer += dt;
        if (_stateSyncTimer >= StateSyncInterval)
        {
            _stateSyncTimer = 0f;
            BroadcastSnapshot();
        }
    }

    private void UpdateTimer(float delta)
    {
        _timeRemaining -= delta;
        EmitSignal(SignalName.TimerUpdated, _timeRemaining);

        if (_timeRemaining <= 0)
        {
            _timeRemaining = 0;
            EndMatch(PlayerRole.Defender); // Time's up -> Defender wins
        }
    }

    private void UpdateEconomy(float delta)
    {
        _incomeTimer += delta;
        if (_incomeTimer >= PassiveIncomeInterval)
        {
            _incomeTimer -= PassiveIncomeInterval;
            AddPassiveIncome();
        }
    }

    private void AddPassiveIncome()
    {
        var peerIds = new List<long>(_playerMoney.Keys);
        foreach (var id in peerIds)
        {
            AddMoney(id, PassiveIncomeAmount);
        }
    }


    public void AddMoney(long peerId, int amount)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        if (!_playerMoney.ContainsKey(peerId))
        {
            _playerMoney[peerId] = 0;
        }

        _playerMoney[peerId] += amount;
        int value = _playerMoney[peerId];
        EmitSignal(SignalName.MoneyChanged, peerId, value);
        Rpc(nameof(SyncMoneyRpc), peerId, value);
    }

    public void DeductMoney(long peerId, int amount)
    {
        AddMoney(peerId, -amount);
    }

    public int GetMoney(long peerId)
    {
        return _playerMoney.GetValueOrDefault(peerId, 0);
    }

    public bool CanAfford(long peerId, int cost)
    {
        return GetMoney(peerId) >= cost;
    }

    public bool TrySpend(long playerId, int amount)
    {
        if (!Multiplayer.IsServer() || amount <= 0) return false;
        if (!CanAfford(playerId, amount)) return false;

        DeductMoney(playerId, amount);
        return true;
    }

    public bool TryBuyUnit(long playerId, UnitData unit)
    {
        if (unit == null) return false;
        int finalCost = Math.Max(0, unit.MatchCost);
        return TrySpend(playerId, finalCost);
    }

    public void TakeBaseDamage(int amount)
    {
        if (!Multiplayer.IsServer() || _currentState != MatchState.Playing || amount <= 0) return;

        _baseHealth -= amount;
        _baseHealth = Math.Max(0, _baseHealth);

        EmitSignal(SignalName.BaseHealthChanged, _baseHealth, MaxBaseHealth);
        BroadcastSnapshot();
        Logger.Info($"MatchManager: Base HP changed: {_baseHealth}/{MaxBaseHealth}");

        if (_baseHealth <= 0)
        {
            EndMatch(PlayerRole.Attacker); // Base destroyed -> Attacker wins
        }
    }

    private void EndMatch(PlayerRole winner)
    {
        if (!Multiplayer.IsServer() || _currentState == MatchState.Ended)
        {
            return;
        }

        _currentState = MatchState.Ended;
        EmitSignal(SignalName.MatchStateChanged, (int)_currentState);
        EmitSignal(SignalName.MatchEnded, (int)winner);

        BroadcastSnapshot();
        Rpc(nameof(SyncMatchEndedRpc), (int)winner);

        Logger.Info($"MatchManager: Match Ended! Winner: {winner}");
    }

    public void RequestFullSync()
    {
        if (Multiplayer.IsServer())
        {
            SendFullSyncToPeer(1);
            return;
        }

        RpcId(1, nameof(RequestFullSyncRpc));
    }

    private void OnPeerConnected(long id)
    {
        if (!Multiplayer.IsServer()) return;
        if (!_playerMoney.ContainsKey(id)) _playerMoney[id] = StartingMoney;
        SendFullSyncToPeer(id);
    }

    private void OnPeerDisconnected(long id)
    {
        if (!Multiplayer.IsServer()) return;
        _playerMoney.Remove(id);
    }

    private void SendFullSyncToPeer(long peerId)
    {
        RpcId(peerId, nameof(SyncSnapshotRpc), (int)_currentState, _timeRemaining, _baseHealth, MaxBaseHealth);
        foreach (var kv in _playerMoney)
        {
            RpcId(peerId, nameof(SyncMoneyRpc), kv.Key, kv.Value);
        }
    }

    private void BroadcastSnapshot()
    {
        Rpc(nameof(SyncSnapshotRpc), (int)_currentState, _timeRemaining, _baseHealth, MaxBaseHealth);
    }

    private void BroadcastAllMoney()
    {
        foreach (var kv in _playerMoney)
        {
            Rpc(nameof(SyncMoneyRpc), kv.Key, kv.Value);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestFullSyncRpc()
    {
        if (!Multiplayer.IsServer()) return;
        long sender = Multiplayer.GetRemoteSenderId();
        if (sender <= 0) return;
        SendFullSyncToPeer(sender);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncSnapshotRpc(int state, float timeRemaining, int baseHealth, int maxBaseHealth)
    {
        _currentState = (MatchState)state;
        _timeRemaining = Math.Max(0f, timeRemaining);
        _baseHealth = Math.Clamp(baseHealth, 0, maxBaseHealth);

        EmitSignal(SignalName.MatchStateChanged, state);
        EmitSignal(SignalName.TimerUpdated, _timeRemaining);
        EmitSignal(SignalName.BaseHealthChanged, _baseHealth, maxBaseHealth);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncMoneyRpc(long peerId, int amount)
    {
        _playerMoney[peerId] = amount;
        EmitSignal(SignalName.MoneyChanged, peerId, amount);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncMatchEndedRpc(int winnerRole)
    {
        _currentState = MatchState.Ended;
        EmitSignal(SignalName.MatchStateChanged, (int)_currentState);
        EmitSignal(SignalName.MatchEnded, winnerRole);
    }

    public MatchState GetCurrentState() => _currentState;
    public float GetTimeRemaining() => _timeRemaining;
    public int GetBaseHealth() => _baseHealth;
}