using Godot;
using System;

public partial class LoginScreen : Control
{
    // Legăturile cu UI-ul (trase din Inspector)
    [Export] public LineEdit EmailInput = null!;
    [Export] public LineEdit PasswordInput = null!;
    [Export] public Button LoginButton = null!;
    [Export] public Label StatusLabel = null!;

    // Calea către scena jocului (o vom modifica când facem harta)
    [Export] public PackedScene MainMenuScene = null!;

    public override void _Ready()
    {
        // Conectăm semnalul de apăsare a butonului
        LoginButton.Pressed += OnLoginPressed;

        // Resetăm statusul
        StatusLabel.Text = "";

        //   "email": "fermier@joc.com",
        //   "password": "Password123!"
        EmailInput.Text = "fermier@joc.com";
        PasswordInput.Text = "Password123!";
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
            bool success = await NetworkManager.Instance.AuthenticateAsync(EmailInput.Text, PasswordInput.Text);

            if (success)
            {
                StatusLabel.Text = "Succes! Se încarcă jocul...";
                StatusLabel.Modulate = Colors.Green;
                await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
                // GetTree().ChangeSceneToFile(GameScenePath);
                GetTree().ChangeSceneToPacked(MainMenuScene);
                return;
            }

            ShowError("Email sau parolă greșită!");
        }
        catch (Exception ex)
        {
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