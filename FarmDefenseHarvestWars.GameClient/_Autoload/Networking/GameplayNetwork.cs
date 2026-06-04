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

        GD.Print($"Player connected (unidentified): {id}");
        // Wait for IdentifyMyself RPC to assign role
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void IdentifyMyself(string email)
    {
        if (!Multiplayer.IsServer()) return;

        long id = Multiplayer.GetRemoteSenderId();
        GD.Print($"Player {id} identifying as: {email}");

        PlayerRole role;
        if (email == CmdArgs.DefenderName)
        {
            role = PlayerRole.Defender;
        }
        else if (email == CmdArgs.AttackerName)
        {
            role = PlayerRole.Attacker;
        }
        else
        {
            GD.PrintErr($"[GameplayNetwork] Unauthorized connection attempt from {email} (Peer {id})");
            _peer?.DisconnectPeer((int)id);
            return;
        }

        // Clean up any old sessions for this role if they exist (different peer id)
        var oldPeersWithSameRole = new List<long>();
        foreach (var kv in _connectedPlayers)
        {
            if (kv.Value == role && kv.Key != id)
            {
                oldPeersWithSameRole.Add(kv.Key);
            }
        }

        foreach (var oldPeerId in oldPeersWithSameRole)
        {
            GD.Print($"[GameplayNetwork] Replacing old session for {role} (Peer {oldPeerId}) with new Peer {id}");
            _connectedPlayers.Remove(oldPeerId);
        }

        _recentlyDisconnectedPlayers.Remove(id);
        _connectedPlayers[id] = role;

        // Sync role back to the identified client
        RpcId(id, nameof(SyncRoleToClient), id, (int)role);

        // Sync other players to this client
        foreach (var existingId in _connectedPlayers.Keys)
        {
            if (existingId != id)
            {
                RpcId(id, nameof(SyncRoleToClient), existingId, (int)_connectedPlayers[existingId]);
            }
        }

        // Announce new identified player to others
        Rpc(nameof(SyncRoleToClient), id, (int)role);

        if (_isMatchStarted)
        {
            GD.Print($"[GameplayNetwork] Peer {id} reconnected to ongoing match. Sending sync RPCs...");
            var defPayload = BuildDeckPayload(CmdArgs.DefenderDeck);
            var atkPayload = BuildDeckPayload(CmdArgs.AttackerDeck);

            RpcId(id, nameof(SyncMatchDecksToClient),
                CmdArgs.MatchId ?? "",
                defPayload.Types, defPayload.Levels,
                atkPayload.Types, atkPayload.Levels,
                CmdArgs.DefenderAvatarIndex, CmdArgs.AttackerAvatarIndex,
                CmdArgs.DefenderName, CmdArgs.AttackerName);
            
            RpcId(id, nameof(StartGameScene));
            RpcId(id, nameof(BroadcastMatchStart));
        }
        else
        {
            CheckGameStart();
        }
    }

    private void OnPlayerDisconnected(long id)
    {
        if (_connectedPlayers.TryGetValue(id, out PlayerRole role))
        {
            _recentlyDisconnectedPlayers[id] = role;
        }

        _connectedPlayers.Remove(id);
        GD.Print($"Player {id} disconnected ({role}).");
    }

    private async void CheckGameStart()
    {
        // Count unique roles instead of just peer count to ensure both sides are ready
        int identifiedRolesCount = 0;
        bool hasDefender = false;
        bool hasAttacker = false;

        foreach (var role in _connectedPlayers.Values)
        {
            if (role == PlayerRole.Defender) hasDefender = true;
            if (role == PlayerRole.Attacker) hasAttacker = true;
        }

        if (hasDefender) identifiedRolesCount++;
        if (hasAttacker) identifiedRolesCount++;

        if (identifiedRolesCount == MaxPlayers)
        {
            var defPayload = BuildDeckPayload(CmdArgs.DefenderDeck);
            var atkPayload = BuildDeckPayload(CmdArgs.AttackerDeck);

            if (!_isMatchStarted)
            {
                GD.Print("Match Ready (Both roles identified)! Sending StartGameScene RPC to everyone...");
                
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
                        bool allReady = true;
                        foreach (var peerId in Multiplayer.GetPeers())
                        {
                            if (!mm.IsClientReady(peerId))
                            {
                                allReady = false;
                                break;
                            }
                        }

                        if (allReady && _connectedPlayers.Count >= MaxPlayers)
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
                Rpc(nameof(BroadcastMatchStart));
            }
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
            EmitSignal(SignalName.ClientJoinStateChanged, true, $"Role {role} confirmed. Waiting for match start...");
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

        EmitSignal(SignalName.ClientJoinStateChanged, true, "Connected. Identifying...");
        
        string email = GameState.Instance?.CurrentProfile?.Email ?? "Anonymous";
        GD.Print($"[GameplayNetwork] Connected to server. Identifying as {email}...");
        RpcId(1, nameof(IdentifyMyself), email);
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