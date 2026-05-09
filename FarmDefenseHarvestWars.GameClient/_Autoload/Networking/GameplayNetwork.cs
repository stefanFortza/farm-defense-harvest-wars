using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using System.Collections.Generic;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Models.Game;

public partial class GameplayNetwork : Node
{
    private ENetMultiplayerPeer? _peer;
    private const int DefaultPort = 7777;
    private const int MaxPlayers = 2; // Constanta e sfanta
    private const double ClientJoinTimeoutSeconds = 10.0;

    // Stocăm ID -> Role
    private readonly System.Collections.Generic.Dictionary<long, PlayerRole> _connectedPlayers = [];
    private readonly System.Collections.Generic.Dictionary<long, PlayerRole> _recentlyDisconnectedPlayers = [];
    private ulong _clientConnectAttemptId;
    private bool _awaitingServerStart;
    private bool _gameSceneLoadRequested;

    [Signal] public delegate void ClientJoinStateChangedEventHandler(bool isConnecting, string message);
    [Signal] public delegate void MatchStartedEventHandler();

    // Proprietate helper pentru a afla rolul meu curent rapid
    public PlayerRole? MyRole =>
        _connectedPlayers.TryGetValue(Multiplayer.GetUniqueId(), out var role) ? role : null;

    public int ConnectedPlayerCount => _connectedPlayers.Count;

    public bool TryGetRoleForPeer(long id, out PlayerRole role)
    {
        if (_connectedPlayers.TryGetValue(id, out role))
        {
            return true;
        }

        return _recentlyDisconnectedPlayers.TryGetValue(id, out role);
    }

    public override void _Ready()
    {
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    public override void _ExitTree()
    {
        Multiplayer.ConnectedToServer -= OnConnectedToServer;
        Multiplayer.ConnectionFailed -= OnConnectionFailed;
        Multiplayer.ServerDisconnected -= OnServerDisconnected;
    }

    public void StartDedicatedServer()
    {
        _peer = new ENetMultiplayerPeer();
        int port = CmdArgs.Port ?? DefaultPort;

        GD.Print(
            $"[GameplayNetwork] Dedicated server startup | IsServer={CmdArgs.IsServer} | Port={port} | MatchId={CmdArgs.MatchId ?? "<null>"} | DefenderDeckSet={CmdArgs.DefenderDeck != null} | AttackerDeckSet={CmdArgs.AttackerDeck != null}");

        var error = _peer.CreateServer(port, MaxPlayers); // Limitam direct din ENet la 2

        if (error != Error.Ok)
        {
            GD.PrintErr($"Server Fail: {error} | attemptedPort={port} | hint=port may already be in use or command-line port parsing failed");
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;

        GD.Print("Server Started. Waiting for players...");

        // Fix: Use CallDeferred to avoid "busy adding/removing children" error
        _gameSceneLoadRequested = true;
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
    }

    public void JoinGameServer(string ip = "127.0.0.1", int port = DefaultPort)
    {
        Disconnect();
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(ip, port);
        if (error != Error.Ok)
        {
            FailClientJoin($"Failed to start client peer: {error}");
            return;
        }

        _awaitingServerStart = true;
        _clientConnectAttemptId++;

        Multiplayer.MultiplayerPeer = _peer;
        EmitSignal(SignalName.ClientJoinStateChanged, true, $"Connecting to {ip}:{port}...");
        _ = WatchClientJoinTimeoutAsync(_clientConnectAttemptId);
    }

    // --- SERVER LOGIC ---

    private void OnPlayerConnected(long id)
    {
        if (!Multiplayer.IsServer()) return;

        GD.Print($"Player connected: {id}");

        PlayerRole newRole = _connectedPlayers.Count == 0 ? PlayerRole.Defender : PlayerRole.Attacker;

        _recentlyDisconnectedPlayers.Remove(id);
        _connectedPlayers[id] = newRole;

        // 3. Trimitem noului jucător lista cu TOȚI jucătorii existenți (ca să știe cine e cine)
        foreach (var existingId in _connectedPlayers.Keys)
        {
            RpcId(id, nameof(SyncRoleToClient), existingId, (int)_connectedPlayers[existingId]);
        }

        // 4. Anunțăm pe ceilalți că a intrat unul nou
        Rpc(nameof(SyncRoleToClient), id, (int)newRole);

        // 5. Verificăm Startul (Fără LINQ, doar numărăm)
        CheckGameStart();
    }

    private void OnPlayerDisconnected(long id)
    {
        if (_connectedPlayers.TryGetValue(id, out PlayerRole role))
        {
            _recentlyDisconnectedPlayers[id] = role;
        }

        _connectedPlayers.Remove(id);
        GD.Print($"Player {id} disconnected.");

        // Opțional: Dacă iese unul, jocul se termină sau se pune pauză
        // networkManager.EndGame("Opponent Disconnected");
    }

    private async void CheckGameStart()
    {
        if (_connectedPlayers.Count == MaxPlayers)
        {
            GD.Print("Match Ready! Sending StartGameScene RPC...");
            
            var defPayload = BuildDeckPayload(CmdArgs.DefenderDeck);
            var atkPayload = BuildDeckPayload(CmdArgs.AttackerDeck);

            Rpc(nameof(SyncMatchDecksToClient),
                CmdArgs.MatchId ?? "",
                defPayload.Types, defPayload.Levels,
                atkPayload.Types, atkPayload.Levels,
                CmdArgs.DefenderAvatarIndex, CmdArgs.AttackerAvatarIndex,
                CmdArgs.DefenderName, CmdArgs.AttackerName);
            Rpc(nameof(StartGameScene));

            // Wait for all clients to be ready (load scene + handshake)
            int maxWaitAttempts = 100; // 10 seconds max
            MatchManager? mm = null;

            while (maxWaitAttempts > 0)
            {
                var matchManagerNode = GetTree().Root.FindChild("MatchManager", true, false);
                if (matchManagerNode is MatchManager foundMm)
                {
                    mm = foundMm;
                    // Check if all connected peers (excluding server) are in the ready list
                    bool allReady = true;
                    foreach (var peerId in Multiplayer.GetPeers())
                    {
                        // We check the internal ready set of MatchManager
                        // Using reflection or making it public. Let's assume we add a helper method.
                        if (!mm.IsClientReady(peerId))
                        {
                            allReady = false;
                            break;
                        }
                    }

                    if (allReady && Multiplayer.GetPeers().Length >= MaxPlayers)
                    {
                        GD.Print("[GameplayNetwork] All clients reported ready. Starting match!");
                        Rpc(nameof(BroadcastMatchStart));
                        return;
                    }
                }

                await Task.Delay(100);
                maxWaitAttempts--;
            }

            GD.PrintErr("[GameplayNetwork] Timeout waiting for clients to be ready!");
            // Fallback: try starting anyway
            Rpc(nameof(BroadcastMatchStart));
        }
    }

    private bool _isMatchStarted;
    public bool IsMatchStarted => _isMatchStarted;

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void BroadcastMatchStart()
    {
        GD.Print("[GameplayNetwork] Global Match Start received.");
        _isMatchStarted = true;
        EmitSignal(SignalName.MatchStarted);
    }

    // --- CLIENT RPCs ---

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncRoleToClient(long id, int roleInt)
    {
        var role = (PlayerRole)roleInt;
        _connectedPlayers[id] = role;

        if (id == Multiplayer.GetUniqueId())
        {
            GameState.Instance?.SetAssignedRole(role);
            EmitSignal(SignalName.ClientJoinStateChanged, true, "Connected. Waiting for match start...");
        }

        GD.Print($"[Sync] Player {id} is assigned {role}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncMatchDecksToClient(string matchId,
        Godot.Collections.Array<int> defenderTypes, Godot.Collections.Array<int> defenderLevels,
        Godot.Collections.Array<int> attackerTypes, Godot.Collections.Array<int> attackerLevels,
        int defenderAvatarIndex, int attackerAvatarIndex,
        string defenderName, string attackerName)
    {
        List<UnitUnlockDto> defenderUnits = [];
        List<UnitUnlockDto> attackerUnits = [];

        for (int i = 0; i < defenderTypes.Count; i++)
        {
            defenderUnits.Add(new UnitUnlockDto
            {
                UnitType = (UnitType)defenderTypes[i],
                Level = defenderLevels[i]
            });
        }

        for (int i = 0; i < attackerTypes.Count; i++)
        {
            attackerUnits.Add(new UnitUnlockDto
            {
                UnitType = (UnitType)attackerTypes[i],
                Level = attackerLevels[i]
            });
        }

        GameState.Instance?.SetMatchInfo(matchId, 
            defenderUnits, attackerUnits, 
            defenderAvatarIndex, attackerAvatarIndex, 
            defenderName, attackerName);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartGameScene()
    {
        if (_gameSceneLoadRequested)
        {
            return;
        }

        _gameSceneLoadRequested = true;
        _awaitingServerStart = false;
        EmitSignal(SignalName.ClientJoinStateChanged, false, "");

        // Cleanup Loading Screen if any
        foreach (var child in GetTree().Root.GetChildren())
        {
            if (child is LoadingScreen ls)
            {
                ls.QueueFree();
            }
        }

        GD.Print("Loading Game World...");
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
    }

    private void OnConnectedToServer()
    {
        if (!_awaitingServerStart)
        {
            return;
        }

        EmitSignal(SignalName.ClientJoinStateChanged, true, "Connected. Waiting for role sync...");
    }

    private void OnConnectionFailed()
    {
        if (!_awaitingServerStart)
        {
            return;
        }

        FailClientJoin("Failed to connect to match server.");
    }

    private void OnServerDisconnected()
    {
        if (!_awaitingServerStart)
        {
            return;
        }

        FailClientJoin("Disconnected while waiting for match start.");
    }

    private async Task WatchClientJoinTimeoutAsync(ulong attemptId)
    {
        await ToSignal(GetTree().CreateTimer(ClientJoinTimeoutSeconds), SceneTreeTimer.SignalName.Timeout);

        if (attemptId != _clientConnectAttemptId || !_awaitingServerStart)
        {
            return;
        }

        FailClientJoin("Timed out waiting for match server response.");
    }

    private static (Godot.Collections.Array<int> Types, Godot.Collections.Array<int> Levels) BuildDeckPayload(IReadOnlyList<UnitUnlockDto>? units)
    {
        Godot.Collections.Array<int> types = [];
        Godot.Collections.Array<int> levels = [];

        if (units == null)
        {
            return (types, levels);
        }

        foreach (var unlock in units)
        {
            types.Add((int)unlock.UnitType);
            levels.Add(unlock.Level);
        }

        return (types, levels);
    }

    public void Disconnect()
    {
        GD.Print("[GameplayNetwork] Disconnecting and resetting state.");
        _isMatchStarted = false;
        _awaitingServerStart = false;
        _gameSceneLoadRequested = false;
        _connectedPlayers.Clear();
        _recentlyDisconnectedPlayers.Clear();

        if (Multiplayer.MultiplayerPeer == _peer)
        {
            Multiplayer.MultiplayerPeer = null;
        }

        _peer?.Close();
        _peer = null;
    }

    private void FailClientJoin(string reason)
    {
        GD.PrintErr($"[GameplayNetwork] {reason}");
        Disconnect();
        EmitSignal(SignalName.ClientJoinStateChanged, false, reason);
    }
}