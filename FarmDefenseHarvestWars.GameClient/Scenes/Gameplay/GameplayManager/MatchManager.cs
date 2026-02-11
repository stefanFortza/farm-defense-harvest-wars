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

    private MatchState _currentState = MatchState.Waiting;
    private float _timeRemaining;
    private int _baseHealth;
    private float _incomeTimer = 0f;

    // PeerID -> Money
    private readonly Dictionary<long, int> _playerMoney = new();

    [Signal] public delegate void MatchStateChangedEventHandler(int newState);
    [Signal] public delegate void MoneyChangedEventHandler(long peerId, int newAmount);
    [Signal] public delegate void BaseHealthChangedEventHandler(int current, int max);
    [Signal] public delegate void MatchEndedEventHandler(int winnerRole);
    [Signal] public delegate void TimerUpdatedEventHandler(float timeRemaining);

    public override void _Ready()
    {
        // Această componentă rulează logică doar pe Server
        if (!Multiplayer.IsServer())
        {
            SetProcess(false);
            return;
        }

        ResetMatch();
    }

    public void ResetMatch()
    {
        _currentState = MatchState.Playing;
        _timeRemaining = MatchDurationSeconds;
        _baseHealth = MaxBaseHealth;
        _playerMoney.Clear();
        _incomeTimer = 0f;

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

        Logger.Info("MatchManager: Match Reset and Started.");
    }

    public override void _Process(double delta)
    {
        if (_currentState != MatchState.Playing) return;

        UpdateTimer((float)delta);
        UpdateEconomy((float)delta);
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
        // Dăm bani tuturor jucătorilor activi
        foreach (var id in _playerMoney.Keys)
        {
            AddMoney(id, PassiveIncomeAmount);
        }
    }


    public void AddMoney(long peerId, int amount)
    {
        if (!_playerMoney.ContainsKey(peerId))
        {
            _playerMoney[peerId] = 0;
        }

        _playerMoney[peerId] += amount;
        EmitSignal(SignalName.MoneyChanged, peerId, _playerMoney[peerId]);
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
        if (!CanAfford(playerId, amount)) return false;

        DeductMoney(playerId, amount);
        return true;
    }

    public bool TryBuyUnit(long playerId, UnitData unit)
    {
        int finalCost = unit.MatchCost;
        return TrySpend(playerId, finalCost);
    }

    public void TakeBaseDamage(int amount)
    {
        if (_currentState != MatchState.Playing) return;

        _baseHealth -= amount;
        _baseHealth = Math.Max(0, _baseHealth);

        EmitSignal(SignalName.BaseHealthChanged, _baseHealth, MaxBaseHealth);
        Logger.Info($"MatchManager: Base HP changed: {_baseHealth}/{MaxBaseHealth}");

        if (_baseHealth <= 0)
        {
            EndMatch(PlayerRole.Attacker); // Base destroyed -> Attacker wins
        }
    }

    private void EndMatch(PlayerRole winner)
    {
        _currentState = MatchState.Ended;
        EmitSignal(SignalName.MatchStateChanged, (int)_currentState);
        EmitSignal(SignalName.MatchEnded, (int)winner);

        Logger.Info($"MatchManager: Match Ended! Winner: {winner}");
    }

    public MatchState GetCurrentState() => _currentState;
    public float GetTimeRemaining() => _timeRemaining;
    public int GetBaseHealth() => _baseHealth;
}