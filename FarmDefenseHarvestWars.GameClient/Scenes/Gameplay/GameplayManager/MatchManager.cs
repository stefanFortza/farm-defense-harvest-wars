using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.GameClient.Entities.Components;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

public partial class MatchManager : Node
{
    public enum MatchState { Waiting, Playing, Ended }

    [Export] public float MatchDurationSeconds = 300f; // 5 minutes
    [Export] public int StartingMoney = 100;
    [Export] public int PassiveIncomeAmount = 5;
    [Export] public float PassiveIncomeInterval = 1.0f;
    [Export] public int MaxBaseHealth = 100;
    [Export] public float StateSyncInterval = 0.2f;
    [Export] public float DisconnectGraceSeconds = 30f;

    [Export] public HealthComponent BaseHealthComponent { get; private set; } = null!;

    private MatchState _currentState = MatchState.Waiting;
    private float _timeRemaining;
    private float _incomeTimer = 0f;
    private float _stateSyncTimer = 0f;
    private bool _completionReported;

    // Local copy for clients, synced from server
    private int _syncedBaseHealth;

    // PeerID -> Money
    private readonly Dictionary<long, int> _playerMoney = new();
    private readonly Dictionary<PlayerRole, ulong> _disconnectRoleTokens = new();
    private readonly HashSet<long> _readyClients = new();

    [Signal] public delegate void MatchStateChangedEventHandler(int newState);
    [Signal] public delegate void MoneyChangedEventHandler(long peerId, int newAmount);
    [Signal] public delegate void BaseHealthChangedEventHandler(int current, int max);
    [Signal] public delegate void MatchEndedEventHandler(int winnerRole);
    [Signal] public delegate void TimerUpdatedEventHandler(float timeRemaining);

    public override void _Ready()
    {
        this.EnsureNotNull(BaseHealthComponent, nameof(BaseHealthComponent));

        // Connect to global signal from Autoload
        NetworkBootstrap.Instance.Gameplay.MatchStarted += OnGlobalMatchStarted;

        if (Multiplayer.IsServer())
        {
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;

            BaseHealthComponent.HealthChanged += OnBaseHealthChanged;
            BaseHealthComponent.Died += OnBaseDestroyed;

            _readyClients.Clear();
            return;
        }

        // Check if match already started globally before we loaded
        if (NetworkBootstrap.Instance.Gameplay.IsMatchStarted)
        {
            OnGlobalMatchStarted();
        }

        SetProcess(false);
        // Notify server that this client has loaded the scene
        RpcId(1, nameof(NotifyClientReady));
        CallDeferred(nameof(RequestFullSync));
    }

    private void OnGlobalMatchStarted()
    {
        _readyClients.Clear();
        if (Multiplayer.IsServer())
        {
            ResetMatch();
        }
        else
        {
            _currentState = MatchState.Playing;
            SetProcess(true);
            Logger.Info("MatchManager: Match Started via Global Signal");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void NotifyClientReady()
    {
        if (!Multiplayer.IsServer()) return;

        long senderId = Multiplayer.GetRemoteSenderId();
        _readyClients.Add(senderId);
        Logger.Info($"[MatchManager] Client {senderId} is ready.");
    }

    public bool IsClientReady(long peerId)
    {
        return _readyClients.Contains(peerId);
    }

    public override void _ExitTree()
    {
        if (NetworkBootstrap.Instance?.Gameplay != null)
        {
            NetworkBootstrap.Instance.Gameplay.MatchStarted -= OnGlobalMatchStarted;
        }

        if (BaseHealthComponent != null)
        {
            BaseHealthComponent.HealthChanged -= OnBaseHealthChanged;
            BaseHealthComponent.Died -= OnBaseDestroyed;
        }

        // Check if multiplayer is still active and valid before unbinding network events
        if (Multiplayer != null && Multiplayer.MultiplayerPeer != null &&
            Multiplayer.MultiplayerPeer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Disconnected)
        {
            if (Multiplayer.IsServer())
            {
                Multiplayer.PeerConnected -= OnPeerConnected;
                Multiplayer.PeerDisconnected -= OnPeerDisconnected;
            }
        }
    }

    public void ResetMatch()
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        _currentState = MatchState.Playing;
        _timeRemaining = MatchDurationSeconds;

        BaseHealthComponent.Initialize(MaxBaseHealth);

        _playerMoney.Clear();
        _disconnectRoleTokens.Clear();
        _incomeTimer = 0f;
        _stateSyncTimer = 0f;
        _completionReported = false;

        foreach (var id in Multiplayer.GetPeers())
        {
            _playerMoney[id] = StartingMoney;
            EmitSignal(SignalName.MoneyChanged, id, StartingMoney);
        }

        _playerMoney[1] = StartingMoney;
        EmitSignal(SignalName.MoneyChanged, 1L, StartingMoney);

        EmitSignal(SignalName.BaseHealthChanged, BaseHealthComponent.CurrentHealth, MaxBaseHealth);
        EmitSignal(SignalName.MatchStateChanged, (int)_currentState);
        EmitSignal(SignalName.TimerUpdated, _timeRemaining);

        BroadcastSnapshot();
        BroadcastAllMoney();

        string matchLog = $"MatchManager: Match Reset and Started";
        if (GameState.Instance.IsMatchConfigured)
        {
            matchLog += $" | Match ID: {GameState.Instance.MatchId} | Defender deck: {string.Join(", ", GameState.Instance.DefenderDeck!)} | Attacker deck: {string.Join(", ", GameState.Instance.AttackerDeck!)}";
        }
        Logger.Info(matchLog);
    }

    private float _quitDelayTimer = 2.0f; // Give clients some time to receive final RPCs
    private float _forceQuitTimer = 20.0f; // Safety timeout if reporting fails

    public override void _Process(double delta)
    {
        if (Multiplayer.MultiplayerPeer == null || !Multiplayer.IsServer()) return;

        if (_currentState == MatchState.Ended)
        {
            // Normal quit logic after successful reporting
            if (_completionReported)
            {
                _quitDelayTimer -= (float)delta;
                if (_quitDelayTimer <= 0)
                {
                    GD.Print("Match ended, completion reported and delay expired. Quitting server.");
                    GetTree().Quit();
                }
            }
            else
            {
                // Safety quit logic if reporting is stuck or failing
                _forceQuitTimer -= (float)delta;
                if (_forceQuitTimer <= 0)
                {
                    GD.PrintErr("Match ended, but completion report is stuck. Force quitting server for safety.");
                    GetTree().Quit();
                }
            }
            return;
        }

        if (_currentState != MatchState.Playing) return;

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
        if (!Multiplayer.IsServer()) return;

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

    // Still useful for manual damage if needed, but primarily handled by HealthComponent
    public void TakeBaseDamage(int amount)
    {
        if (!Multiplayer.IsServer() || _currentState != MatchState.Playing || amount <= 0) return;
        BaseHealthComponent.TakeDamage(amount);
    }

    private void OnBaseHealthChanged(int current, int max)
    {
        EmitSignal(SignalName.BaseHealthChanged, current, max);
        BroadcastSnapshot();
        Logger.Info($"MatchManager: Base HP changed via HealthComponent: {current}/{max}");
    }

    private void OnBaseDestroyed()
    {
        EndMatch(PlayerRole.Attacker);
    }

    private void EndMatch(PlayerRole winner)
    {
        EndMatch(winner, "normal", false);
    }

    private void EndMatch(PlayerRole winner, string terminationReason, bool isAborted)
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
        _ = ReportCompletionAsync(winner, terminationReason, isAborted);

        Logger.Info($"MatchManager: Match Ended! Winner: {winner} | Reason: {terminationReason} | IsAborted={isAborted}");
    }

    public void RequestFullSync()
    {
        if (Multiplayer.IsServer())
        {
            SendFullSyncToPeer(1);
            return;
        }

        // Fix: Check if peer is actually connected before sending RPC
        var peer = Multiplayer.MultiplayerPeer;
        if (peer == null || peer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
        {
            Logger.Info("MatchManager: Deferring RequestFullSync until connected to server...");

            // Use Action for C# signal events
            Action onConnected = null;
            onConnected = () =>
            {
                Logger.Info("MatchManager: Connected to server, sending deferred RequestFullSync.");
                RpcId(1, nameof(RequestFullSyncRpc));
                Multiplayer.ConnectedToServer -= onConnected;
            };

            Multiplayer.ConnectedToServer += onConnected;
            return;
        }

        RpcId(1, nameof(RequestFullSyncRpc));
    }

    private void OnPeerConnected(long id)
    {
        if (!Multiplayer.IsServer()) return;

        if (!_playerMoney.ContainsKey(id))
        {
            _playerMoney[id] = StartingMoney;
        }

        if (_currentState == MatchState.Playing)
        {
            foreach (PlayerRole role in new List<PlayerRole>(_disconnectRoleTokens.Keys))
            {
                _disconnectRoleTokens[role]++;
            }
        }

        SendFullSyncToPeer(id);
    }

    private void OnPeerDisconnected(long id)
    {
        if (!Multiplayer.IsServer()) return;

        _playerMoney.Remove(id);

        if (_currentState != MatchState.Playing) return;

        GameplayNetwork? gameplay = NetworkBootstrap.Instance?.Gameplay;
        if (gameplay == null || !gameplay.TryGetRoleForPeer(id, out PlayerRole disconnectedRole))
        {
            Logger.Error($"MatchManager: Could not resolve disconnected peer role for peer {id}; skipping forfeit flow.");
            return;
        }

        if (gameplay.ConnectedPlayerCount == 0)
        {
            EndMatch(PlayerRole.Defender, "both_players_disconnected", true);
            Logger.Info($"MatchManager: Peer {id} ({disconnectedRole}) disconnected. Both players are now gone. Match ended immediately as aborted.");
            return;
        }

        ulong nextToken = _disconnectRoleTokens.TryGetValue(disconnectedRole, out ulong currentToken)
            ? currentToken + 1
            : 1;

        _disconnectRoleTokens[disconnectedRole] = nextToken;
        _ = WatchDisconnectGraceAsync(disconnectedRole, nextToken, id);

        Logger.Info($"MatchManager: Peer {id} ({disconnectedRole}) disconnected. Waiting {DisconnectGraceSeconds:0.##}s grace period before forfeit.");
    }

    private async Task WatchDisconnectGraceAsync(PlayerRole disconnectedRole, ulong token, long peerId)
    {
        await ToSignal(GetTree().CreateTimer(DisconnectGraceSeconds), SceneTreeTimer.SignalName.Timeout);

        if (_currentState != MatchState.Playing) return;

        if (!_disconnectRoleTokens.TryGetValue(disconnectedRole, out ulong currentToken) || currentToken != token)
        {
            return;
        }

        GameplayNetwork? gameplay = NetworkBootstrap.Instance?.Gameplay;
        if (gameplay != null && gameplay.ConnectedPlayerCount >= 2) return;

        PlayerRole winner = disconnectedRole == PlayerRole.Defender
            ? PlayerRole.Attacker
            : PlayerRole.Defender;

        EndMatch(winner, $"{disconnectedRole.ToString().ToLowerInvariant()}_disconnect_timeout", false);
        Logger.Info($"MatchManager: Disconnect grace expired for peer {peerId} ({disconnectedRole}). Forfeit winner: {winner}.");
    }

    private async Task ReportCompletionAsync(PlayerRole winner, string terminationReason, bool isAborted)
    {
        if (_completionReported) return;

        string? matchId = GameState.Instance?.MatchId;
        if (string.IsNullOrWhiteSpace(matchId))
        {
            Logger.Error("MatchManager: Missing match id for completion callback.");
            return;
        }

        if (NetworkBootstrap.Instance?.ApiClient == null)
        {
            Logger.Error("MatchManager: API client unavailable for completion callback.");
            return;
        }

        try
        {
            var request = new MatchCompletionRequestDto
            {
                WinnerRole = winner,
                TerminationReason = terminationReason,
                IsAborted = isAborted,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };

            await NetworkBootstrap.Instance.ApiClient.CompleteMatchAsync(
                matchId,
                request,
                CmdArgs.MatchServerCallbackKey);

            _completionReported = true;
            Logger.Info($"MatchManager: Completion callback sent for match {matchId}.");
        }
        catch (Exception ex)
        {
            _completionReported = false;
            Logger.Error($"MatchManager: Failed to send completion callback for match {matchId}. Error: {ex.Message}");
        }
    }

    private void SendFullSyncToPeer(long peerId)
    {
        int currentHp = Multiplayer.IsServer() ? BaseHealthComponent.CurrentHealth : _syncedBaseHealth;
        RpcId(peerId, nameof(SyncSnapshotRpc), (int)_currentState, _timeRemaining, currentHp, MaxBaseHealth);
        foreach (var kv in _playerMoney)
        {
            RpcId(peerId, nameof(SyncMoneyRpc), kv.Key, kv.Value);
        }
    }

    private void BroadcastSnapshot()
    {
        int currentHp = Multiplayer.IsServer() ? BaseHealthComponent.CurrentHealth : _syncedBaseHealth;
        Rpc(nameof(SyncSnapshotRpc), (int)_currentState, _timeRemaining, currentHp, MaxBaseHealth);
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
        _syncedBaseHealth = Math.Clamp(baseHealth, 0, maxBaseHealth);

        // Sync the local component so visual entities (like DefenderBase) can listen to it
        if (!Multiplayer.IsServer() && BaseHealthComponent != null)
        {
            BaseHealthComponent.SetHealthSilently(_syncedBaseHealth, maxBaseHealth);
        }

        EmitSignal(SignalName.MatchStateChanged, state);
        EmitSignal(SignalName.TimerUpdated, _timeRemaining);
        EmitSignal(SignalName.BaseHealthChanged, _syncedBaseHealth, maxBaseHealth);
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
        GD.Print($"[MatchManager] SyncMatchEndedRpc received. Winner role: {winnerRole}");
        _currentState = MatchState.Ended;
        EmitSignal(SignalName.MatchStateChanged, (int)_currentState);
        EmitSignal(SignalName.MatchEnded, winnerRole);

        // If we are a client, we no longer need the Godot server connection.
        // The rewards will be fetched from the .NET Backend API.
        if (!Multiplayer.IsServer())
        {
            SetProcess(false);
            NetworkBootstrap.Instance?.Gameplay?.Disconnect();
        }
    }

    public MatchState GetCurrentState() => _currentState;
    public float GetTimeRemaining() => _timeRemaining;
    public int GetBaseHealth() => Multiplayer.IsServer() ? BaseHealthComponent.CurrentHealth : _syncedBaseHealth;
}
