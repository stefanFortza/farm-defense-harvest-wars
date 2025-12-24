using Godot;
using Refit;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.API; // Interfața din Shared

public partial class NetworkManager : Node
{
    // 1. Singleton Pattern - Ca să îl apelezi cu NetworkManager.Instance de oriunde
    public static NetworkManager Instance { get; private set; } = null!;

    // 2. Clientul Refit - Aici trăiește conexiunea
    public IGameApi Api { get; private set; } = null!;

    // 3. Token-ul - Aici îl ținem minte pe durata jocului
    private string _accessToken = "";

    public override void _Ready()
    {
        // Ne asigurăm că există doar unul
        Instance = this;

        // Configurăm Refit o singură dată la pornirea jocului
        var httpClient = new System.Net.Http.HttpClient
        {
            // Folosește portul tău din backend
            BaseAddress = new Uri("http://localhost:5177")
        };

        Api = RestService.For<IGameApi>(httpClient, new RefitSettings
        {
            // Injectăm automat Token-ul la fiecare cerere, dacă îl avem
            AuthorizationHeaderValueGetter = (_, __) => Task.FromResult(_accessToken)
        });
    }

    // Funcție ca să salvăm tokenul după Login
    public void SetToken(string token)
    {
        _accessToken = token;
        GD.Print("Token salvat în NetworkManager!");
    }

    // Funcție de Logout (doar ștergem tokenul)
    public void Logout()
    {
        _accessToken = "";
        GetTree().ChangeSceneToFile("res://Scenes/Authentication/LoginScreen.tscn");
    }
}