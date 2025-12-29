using Godot;
using Refit;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.API; // Interfața din Shared
using FarmDefenseHarvestWars.Shared.Models.Auth;

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