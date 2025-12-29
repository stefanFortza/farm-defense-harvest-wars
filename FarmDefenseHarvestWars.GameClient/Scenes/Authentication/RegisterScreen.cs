using Godot;
using Refit;
using System;
using FarmDefenseHarvestWars.Shared.Models.Auth;

public partial class RegisterScreen : Control
{
    [Export] public LineEdit EmailInput;
    [Export] public LineEdit PasswordInput;
    [Export] public LineEdit ConfirmPasswordInput; // Câmp nou
    [Export] public Button RegisterButton;
    [Export] public Button BackButton;
    [Export] public Label StatusLabel;

    public override void _Ready()
    {
        RegisterButton.Pressed += OnRegisterPressed;
        BackButton.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/Authentication/LoginScreen.tscn");
        StatusLabel.Text = "";
    }

    private async void OnRegisterPressed()
    {
        // 1. Validări Locale (Client-Side)
        if (string.IsNullOrWhiteSpace(EmailInput.Text) || string.IsNullOrWhiteSpace(PasswordInput.Text))
        {
            StatusLabel.Text = "Completează toate câmpurile!";
            StatusLabel.Modulate = Colors.Red;
            return;
        }

        if (PasswordInput.Text != ConfirmPasswordInput.Text)
        {
            StatusLabel.Text = "Parolele nu coincid!";
            StatusLabel.Modulate = Colors.Red;
            return;
        }

        RegisterButton.Disabled = true;
        StatusLabel.Text = "Se creează contul...";
        StatusLabel.Modulate = Colors.Yellow;

        try
        {
            // 2. Pregătim datele
            var request = new RegisterRequestDto
            {
                Email = EmailInput.Text,
                Password = PasswordInput.Text
            };

            // 3. Apelăm API-ul prin NetworkManager
            // Refit va arunca eroare dacă primește altceva în afară de 200 OK
            await NetworkManager.Instance.Api.RegisterAsync(request);

            // 4. Succes!
            StatusLabel.Text = "Cont creat cu succes!";
            StatusLabel.Modulate = Colors.Green;

            GD.Print("User înregistrat.");

            // Așteptăm 1 secundă și trimitem userul la Login
            await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
            GetTree().ChangeSceneToFile("res://Scenes/Authentication/LoginScreen.tscn");
        }
        catch (ApiException ex)
        {
            // Erori de la server (ex: Email deja existent, Parola prea slabă)
            // Identity trimite erorile într-un format specific (ProblemDetails), 
            // dar pentru simplitate afișăm doar codul sau un mesaj generic.

            StatusLabel.Text = "Eroare la înregistrare (ex: Email luat/Parola slaba)";
            StatusLabel.Modulate = Colors.Red;
            GD.PrintErr(ex.Content); // Vezi eroarea exactă în consolă
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Eroare de conexiune.";
            StatusLabel.Modulate = Colors.Red;
        }
        finally
        {
            RegisterButton.Disabled = false;
        }
    }
}
