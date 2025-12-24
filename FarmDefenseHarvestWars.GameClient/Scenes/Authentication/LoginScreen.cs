using Godot;
using System;
using Refit; // Necesar pentru a prinde erorile specifice API
using FarmDefenseHarvestWars.Shared.Models.Auth; // Aici sunt DTO-urile tale

public partial class LoginScreen : Control
{
    // Legăturile cu UI-ul (trase din Inspector)
    [Export] public LineEdit EmailInput = null!;
    [Export] public LineEdit PasswordInput = null!;
    [Export] public Button LoginButton = null!;
    [Export] public Label StatusLabel = null!;

    // Calea către scena jocului (o vom modifica când facem harta)
    private const string GameScenePath = "res://Scenes/Gameplay/FarmMap.tscn";

    public override void _Ready()
    {
        // Conectăm semnalul de apăsare a butonului
        LoginButton.Pressed += OnLoginPressed;

        // Resetăm statusul
        StatusLabel.Text = "";
    }

    private async void OnLoginPressed()
    {
        // 1. UX: Dezactivăm butonul ca să nu apese de 10 ori
        LoginButton.Disabled = true;
        StatusLabel.Text = "Se conectează...";
        StatusLabel.Modulate = Colors.Yellow; // Facem textul galben

        // 2. Validare simplă locală
        if (string.IsNullOrWhiteSpace(EmailInput.Text) || string.IsNullOrWhiteSpace(PasswordInput.Text))
        {
            ShowError("Introdu email și parolă!");
            return;
        }

        try
        {
            // 3. Creăm DTO-ul (Strongly Typed)
            var loginRequest = new LoginRequestDto
            {
                Email = EmailInput.Text,
                Password = PasswordInput.Text
            };

            // 4. Apelăm Singleton-ul (Refit face request-ul în spate)
            // Variabila 'response' va fi automat de tip LoginResponseDto
            var response = await NetworkManager.Instance.Api.LoginAsync(loginRequest);

            // 5. SUCCES: Salvăm token-ul în memorie
            NetworkManager.Instance.SetToken(response.AccessToken);

            StatusLabel.Text = "Succes! Se încarcă jocul...";
            StatusLabel.Modulate = Colors.Green;

            // Așteptăm puțin să vadă utilizatorul mesajul (opțional)
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

            // 6. Schimbăm scena
            GetTree().ChangeSceneToFile(GameScenePath);
        }
        catch (ApiException ex)
        {
            // Aici prindem erorile de la Server (ex: 401 Unauthorized - Parolă greșită)
            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ShowError("Email sau parolă greșită!");
            }
            else
            {
                ShowError($"Eroare server: {ex.StatusCode}");
                GD.PrintErr(ex.Content); // Vedem detalii în consolă
            }
        }
        catch (Exception ex)
        {
            // Aici prindem erorile de conexiune (ex: Serverul e oprit)
            ShowError("Nu se poate conecta la server.");
            GD.PrintErr(ex.Message);
        }
        finally
        {
            // Reactivăm butonul indiferent ce se întâmplă
            LoginButton.Disabled = false;
        }
    }

    private void ShowError(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.Modulate = Colors.Red; // Facem textul roșu
        LoginButton.Disabled = false;
    }
}