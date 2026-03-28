using Godot;
using Refit;
using System;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.API;

public partial class NetworkBootstrap : Node
{
    public static NetworkBootstrap Instance { get; private set; } = null!;

    // Clientul HTTP Refit partajat
    public IGameApi ApiClient { get; private set; } = null!;
    public string AccessToken { get; set; } = "";

    // Referințe către serviciile copil
    public AuthService Auth { get; private set; } = null!;
    public GameplayNetwork Gameplay { get; private set; } = null!;
    public MenuNetwork Menu { get; private set; } = null!;
    // public ShopService Shop { get; private set; } // De implementat ulterior

    public override void _Ready()
    {
        Instance = this;

        // 1. Configurare HTTP
        ApiClient = RestService.For<IGameApi>("http://localhost:5177", new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, __) => Task.FromResult(AccessToken)
        });

        // 2. Inițializare Servicii (le adăugăm ca noduri copil pentru a putea folosi funcții Godot)
        Auth = new AuthService
        {
            Name = "AuthService"
        };
        AddChild(Auth);

        Gameplay = new GameplayNetwork
        {
            Name = "GameplayNetwork"
        };
        AddChild(Gameplay);

        Menu = new MenuNetwork
        {
            Name = "MenuNetwork"
        };
        AddChild(Menu);

        GD.Print("Network Services Initialized.");
    }
}