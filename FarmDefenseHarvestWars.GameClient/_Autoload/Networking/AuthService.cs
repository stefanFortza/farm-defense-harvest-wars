using Godot;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Models.Auth;
using FarmDefenseHarvestWars.Shared.Enums;
using Refit;
using System;


public partial class AuthService : Node
{
    // --- WRAPPER LOGIN: authenticate + load profile into GameState ---
    public async Task<bool> LoginAsync(string email, string password)
    {
        var api = NetworkBootstrap.Instance.ApiClient;
        var loginReq = new LoginRequestDto { Email = email, Password = password };
        var loginRes = await api.LoginAsync(loginReq);

        // Store access token for future requests
        NetworkBootstrap.Instance.AccessToken = loginRes.AccessToken;

        // Fetch profile using the authenticated client
        var profile = await api.GetProfileAsync();

        // Update global game state
        GameState.Instance.SetProfile(profile);

        await LoadPersistedDecksAsync();

        GD.Print("User authenticated and profile loaded.");
        return true;
    }

    public async Task<bool> RegisterAsync(string email, string password)
    {
        var api = NetworkBootstrap.Instance.ApiClient;
        var registerReq = new RegisterRequestDto { Email = email, Password = password };
        await api.RegisterAsync(registerReq);
        GD.Print("User registered successfully.");
        await LoginAsync(email, password); // Auto-login after registration
        return true;
    }

    public async Task LoadPersistedDecksAsync()
    {
        var api = NetworkBootstrap.Instance.ApiClient;

        try
        {
            var defenderDeck = await api.GetDeckAsync(PlayerRole.Defender);
            GameState.Instance.SetDeckForRole(PlayerRole.Defender, defenderDeck.Units);

            var attackerDeck = await api.GetDeckAsync(PlayerRole.Attacker);
            GameState.Instance.SetDeckForRole(PlayerRole.Attacker, attackerDeck.Units);
        }
        catch (ApiException ex)
        {
            GD.PrintErr($"Failed to load persisted decks: {ex.Message}");
        }
    }

    // Funcție de Logout (ștergem tokenul și starea jocului)
    public void Logout()
    {
        NetworkBootstrap.Instance.AccessToken = "";
        GameState.Instance.ClearState();
    }
}