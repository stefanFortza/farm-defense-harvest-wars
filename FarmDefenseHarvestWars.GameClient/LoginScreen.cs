using Godot;
using System;
using NetHttp = System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
// Importăm DTO-urile din Shared (Trebuie să meargă dacă ai făcut referința corect)
using FarmDefenseHarvestWars.Shared.Models;

public partial class LoginScreen : Control
{
    [Export] public LineEdit EmailInput;
    [Export] public LineEdit PasswordInput;
    [Export] public Button LoginButton;
    [Export] public Label StatusLabel;

    // Adresa Backend-ului (Verifică portul în terminalul dotnet run!)
    private const string BaseUrl = "http://localhost:5177";
    private static readonly NetHttp.HttpClient _client = new NetHttp.HttpClient();

    public override void _Ready()
    {
        // Legăm butonul de funcție
        LoginButton.Pressed += OnLoginPressed;
    }

    private async void OnLoginPressed()
    {
        LoginButton.Disabled = true;
        StatusLabel.Text = "Se conectează...";

        string email = EmailInput.Text;
        string password = PasswordInput.Text;

        try
        {
            // 1. LOGIN - Trimitem cererea
            var loginData = new { email = email, password = password };
            var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", loginData);

            if (!response.IsSuccessStatusCode)
            {
                StatusLabel.Text = "Eroare Login! Verifică datele.";
                LoginButton.Disabled = false;
                return;
            }

            // 2. Parsăm răspunsul ca să luăm Token-ul
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);
            string token = jsonDoc.RootElement.GetProperty("accessToken").GetString();

            StatusLabel.Text = "Logat! Se încarcă profilul...";

            // 3. GET PROFILE - Folosim tokenul pentru a cere datele securizate
            _client.DefaultRequestHeaders.Authorization =
                new NetHttp.Headers.AuthenticationHeaderValue("Bearer", token);

            var profileResponse = await _client.GetFromJsonAsync<PlayerProfileDto>($"{BaseUrl}/api/Game/profile");

            if (profileResponse != null)
            {
                StatusLabel.Text = $"Salut {profileResponse.Email}!\nAi {profileResponse.Gold} Aur și Nivelul {profileResponse.Level}.";
                GD.Print($"User Logat: {profileResponse.Email}, Gold: {profileResponse.Gold}");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Eroare de conexiune: {ex.Message}";
            GD.PrintErr(ex);
        }
        finally
        {
            LoginButton.Disabled = false;
        }
    }
}
