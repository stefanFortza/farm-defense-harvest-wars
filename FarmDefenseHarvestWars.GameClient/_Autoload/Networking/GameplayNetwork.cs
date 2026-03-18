using Godot;
using FarmDefenseHarvestWars.Shared.Enums;
using System.Collections.Generic;

public partial class GameplayNetwork : Node
{
    private ENetMultiplayerPeer _peer = null!;
    private const int Port = 7777;
    private const int MaxPlayers = 2; // Constanta e sfanta

    // Stocăm ID -> Role
    private readonly Dictionary<long, PlayerRole> _connectedPlayers = [];

    // Proprietate helper pentru a afla rolul meu curent rapid
    public PlayerRole MyRole => _connectedPlayers.GetValueOrDefault(Multiplayer.GetUniqueId(), PlayerRole.Spectator);

    public void StartDedicatedServer()
    {
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(Port, MaxPlayers); // Limitam direct din ENet la 2

        if (error != Error.Ok)
        {
            GD.PrintErr($"Server Fail: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;

        GD.Print("Server Started. Waiting for players...");
    }

    public void JoinGameServer(string ip = "127.0.0.1")
    {
        _peer = new ENetMultiplayerPeer();
        _peer.CreateClient(ip, Port);
        Multiplayer.MultiplayerPeer = _peer;
    }

    // --- SERVER LOGIC ---

    private void OnPlayerConnected(long id)
    {
        if (!Multiplayer.IsServer()) return;

        GD.Print($"Player connected: {id}");

        PlayerRole newRole = _connectedPlayers.Count == 0 ? PlayerRole.Defender : PlayerRole.Attacker;

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
        _connectedPlayers.Remove(id);
        GD.Print($"Player {id} disconnected.");

        // Opțional: Dacă iese unul, jocul se termină sau se pune pauză
        // networkManager.EndGame("Opponent Disconnected");
    }

    private void CheckGameStart()
    {
        if (_connectedPlayers.Count == MaxPlayers)
        {
            GD.Print("Match Ready! Starting in 1s...");
            // GetTree().CreateTimer(1.0).Timeout += () => Rpc(nameof(StartGameScene));
            Rpc(nameof(StartGameScene));
        }
    }

    // --- CLIENT RPCs ---

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncRoleToClient(long id, int roleInt)
    {
        _connectedPlayers[id] = (PlayerRole)roleInt;
        GD.Print($"[Sync] Player {id} is assigned {(PlayerRole)roleInt}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartGameScene()
    {
        GD.Print("Loading Deck Selection...");
        GetTree().ChangeSceneToFile("res://Scenes/Menus/DeckSelection/DeckSelection.tscn");
    }
}