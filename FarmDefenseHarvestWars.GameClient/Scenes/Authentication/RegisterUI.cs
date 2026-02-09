using Godot;
using Refit;
using System;
using FarmDefenseHarvestWars.Shared.Models.Auth;

public partial class RegisterUI : Control
{
    [Signal] public delegate void RegisterSuccessEventHandler();
    [Signal] public delegate void BackToLoginEventHandler();

    [Export] public LineEdit EmailInput { get; set; } = null!;
    [Export] public LineEdit PasswordInput { get; set; } = null!;
    [Export] public LineEdit ConfirmPasswordInput { get; set; } = null!;
    [Export] public Label ErrorLabel { get; set; } = null!;
    [Export] public Button RegisterButton { get; set; } = null!;

    public override void _Ready()
    {
        ErrorLabel?.Text = "";
        RegisterButton?.Pressed += OnRegisterPressed;
    }

    public async void OnRegisterPressed()
    {
        // 1. Local Validation
        if (string.IsNullOrWhiteSpace(EmailInput.Text) || string.IsNullOrWhiteSpace(PasswordInput.Text))
        {
            ShowMessage("Please fill all fields!", Colors.Red);
            return;
        }

        if (PasswordInput.Text != ConfirmPasswordInput.Text)
        {
            ShowMessage("Passwords do not match!", Colors.Red);
            return;
        }

        RegisterButton?.Disabled = true;
        ShowMessage("Creating account...", Colors.Yellow);

        try
        {
            // 3. Call API
            await NetworkBootstrap.Instance.Auth.RegisterAsync(EmailInput.Text, PasswordInput.Text);

            // 4. Success
            ShowMessage("Account created successfully!", Colors.Green);
            GD.Print("User registered.");

            await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);

            // Navigate back to login
            EmitSignal(SignalName.BackToLogin);
        }
        catch (ApiException ex)
        {
            ShowMessage("Registration failed (e.g. Email taken)", Colors.Red);
            GD.PrintErr(ex.Content);
        }
        catch (Exception ex)
        {
            ShowMessage("Connection error.", Colors.Red);
            GD.PrintErr(ex.Message);
        }
        finally
        {
            RegisterButton?.Disabled = false;
        }
    }

    public void OnBackToLoginPressed()
    {
        EmitSignal(SignalName.BackToLogin);
    }

    private void ShowMessage(string message, Color color)
    {
        if (ErrorLabel != null)
        {
            ErrorLabel.Text = message;
            ErrorLabel.Modulate = color;
        }
    }
}
