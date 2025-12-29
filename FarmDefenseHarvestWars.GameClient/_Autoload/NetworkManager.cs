using Godot;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.API; // Interfața din Shared
using FarmDefenseHarvestWars.Shared.Models.Auth;
using FarmDefenseHarvestWars.Shared.Enums;

public partial class NetworkManager : Node
{
    // 1. Singleton Pattern - Ca să îl apelezi cu NetworkManager.Instance de oriunde
    public static NetworkManager Instance { get; private set; } = null!;

    // 2. Clientul Refit - Aici trăiește conexiunea
    public IGameApi Api { get; private set; } = null!;

    // 3. Token-ul - Aici îl ținem minte pe durata jocului
    private string _accessToken = "";

    // 4. Multiplayer Peer (ENet) - Pentru Gameplay
    private ENetMultiplayerPeer _peer = null!;
    private const int Port = 7777;

    // 5. Game State Management
    private Dictionary<long, PlayerRole> _connectedPlayers = [];
    public PlayerRole GetCurrentRole()
    {
        long myId = Multiplayer.GetUniqueId();
        if (_connectedPlayers.ContainsKey(myId))
        {
            return _connectedPlayers[myId];
        }
        return PlayerRole.Spectator; // Default/Fallback
    }
    public override void _Ready()
    {
        // Ne asigurăm că există doar unul
        Instance = this;

        // // Configurăm Refit o singură dată la pornirea jocului
        // var httpClient = new System.Net.Http.HttpClient
        // {
        //     // Folosește portul tău din backend
        //     BaseAddress = new Uri("http://localhost:5177")
        // };

        Api = RestService.For<IGameApi>("http://localhost:5177", new RefitSettings
        {
            // Injectăm automat Token-ul la fiecare cerere, dacă îl avem
            AuthorizationHeaderValueGetter = (_, __) => Task.FromResult(_accessToken)
        });
    }

    // --- MULTIPLAYER (ENet) ---

    // Așa pornește Serverul (Dedicated Server - Headless)
    public void StartServer()
    {
        if (_peer != null && _peer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Disconnected)
        {
            GD.Print("Server already started.");
            return;
        }

        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(Port);

        if (error != Error.Ok)
        {
            GD.PrintErr($"Nu s-a putut crea serverul: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        GD.Print("Server pornit pe portul " + Port);

        // Evenimente pentru când intră jucătorii
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
    }

    // Așa se conectează Clientul (Jucătorul)
    public void JoinGame(string ipAddress = "127.0.0.1")
    {
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(ipAddress, Port);

        if (error != Error.Ok)
        {
            GD.PrintErr($"Nu m-am putut conecta: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        GD.Print("Încercare conectare la " + ipAddress);
    }

    private void OnPlayerConnected(long id)
    {
        // Serverul vede cine a intrat (ID-ul unic)
        GD.Print($"Jucător conectat cu ID: {id}");

        // Only Server assigns roles
        if (Multiplayer.IsServer())
        {
            AssignRole(id);
        }
    }

    private void OnPlayerDisconnected(long id)
    {
        GD.Print($"Jucător deconectat: {id}");
        if (_connectedPlayers.ContainsKey(id))
        {
            _connectedPlayers.Remove(id);
        }
    }

    private void AssignRole(long id)
    {
        // First player is Defender, Second is Attacker
        PlayerRole role = _connectedPlayers.Count == 0 ? PlayerRole.Defender : PlayerRole.Attacker;

        if (_connectedPlayers.Count >= 2)
        {
            role = PlayerRole.Spectator; // Or kick
        }

        _connectedPlayers[id] = role;
        GD.Print($"Assigned Role {role} to Player {id}");

        // Notify everyone about the new player's role (RPC)
        Rpc(nameof(SyncPlayerRole), id, (int)role);

        // If we have 2 players, Start Game
        if (_connectedPlayers.Values.Any(r => r == PlayerRole.Defender) &&
            _connectedPlayers.Values.Any(r => r == PlayerRole.Attacker))
        {
            GD.Print("Both players connected! Starting Game...");
            // Delay slightly to ensure connection is stable
            GetTree().CreateTimer(1.0f).Timeout += () => Rpc(nameof(StartGame));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncPlayerRole(long id, int roleInt)
    {
        PlayerRole role = (PlayerRole)roleInt;
        _connectedPlayers[id] = role;
        GD.Print($"Client: Player {id} is {role}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartGame()
    {
        GD.Print("Starting Game Scene...");
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld.tscn");
    }

    // --- WRAPPER LOGIN: authenticate + load profile into GameState ---
    public async Task<bool> AuthenticateAsync(string email, string password)
    {
        try
        {
            var loginReq = new LoginRequestDto { Email = email, Password = password };
            var loginRes = await Api.LoginAsync(loginReq);

            // Save token for subsequent requests
            SetToken(loginRes.AccessToken);

            // Fetch profile using the authenticated client
            var profile = await Api.GetProfileAsync();

            // Update global game state
            GameState.Instance.SetProfile(profile);

            GD.Print("User authenticated and profile loaded.");
            return true;
        }
        catch (ApiException apiEx)
        {
            GD.PrintErr($"Auth failed (API): {apiEx.StatusCode} - {apiEx.Content}");
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Auth failed: {ex.Message}");
            return false;
        }
    }

    // Funcție ca să salvăm tokenul după Login (folosit dacă faci login manual)
    public void SetToken(string token)
    {
        _accessToken = token;
        GD.Print("Token salvat în NetworkManager!");
    }

    // Funcție de Logout (ștergem tokenul și starea jocului)
    public void Logout()
    {
        _accessToken = "";
        GameState.Instance.ClearState();
    }
}