using Godot;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Models.Auth;
using Refit;
using System;


public partial class AuthService : Node
{
    // --- WRAPPER LOGIN: authenticate + load profile into GameState ---
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
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

    public async Task<bool> RegisterAsync(string email, string password)
    {
        try
        {
            var api = NetworkBootstrap.Instance.ApiClient;
            var registerReq = new RegisterRequestDto { Email = email, Password = password };
            await api.RegisterAsync(registerReq);
            GD.Print("User registered successfully.");
            await LoginAsync(email, password); // Auto-login after registration
            return true;
        }
        catch (ApiException apiEx)
        {
            GD.PrintErr($"Registration failed (API): {apiEx.StatusCode} - {apiEx.Content}");
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Registration failed: {ex.Message}");
            return false;
        }
    }

    // Funcție de Logout (ștergem tokenul și starea jocului)
    public void Logout()
    {
        NetworkBootstrap.Instance.AccessToken = "";
        GameState.Instance.ClearState();
    }
}