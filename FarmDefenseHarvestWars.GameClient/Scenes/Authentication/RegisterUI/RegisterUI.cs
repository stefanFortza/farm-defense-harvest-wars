using Godot;
using Refit;
using System;
using FarmDefenseHarvestWars.Shared.Models.Auth;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

public partial class RegisterUI : Control
{
    [Signal] public delegate void RegisterSuccessEventHandler();

    [Export] public LineEdit EmailInput { get; set; } = null!;
    [Export] public LineEdit PasswordInput { get; set; } = null!;
    [Export] public LineEdit ConfirmPasswordInput { get; set; } = null!;
    [Export] public Label ErrorLabel { get; set; } = null!;
    [Export] public Button RegisterButton { get; set; } = null!;

    public override void _Ready()
    {
        this.EnsureNotNull(EmailInput, nameof(EmailInput));
        this.EnsureNotNull(PasswordInput, nameof(PasswordInput));
        this.EnsureNotNull(ConfirmPasswordInput, nameof(ConfirmPasswordInput));
        this.EnsureNotNull(ErrorLabel, nameof(ErrorLabel));
        this.EnsureNotNull(RegisterButton, nameof(RegisterButton));

        ErrorLabel.Text = "";
        RegisterButton.Pressed += OnRegisterPressed;
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

        RegisterButton.Disabled = true;
        ShowMessage("Creating account...", Colors.Yellow);

        try
        {
            // 3. Call API
            await NetworkBootstrap.Instance.Auth.RegisterAsync(EmailInput.Text, PasswordInput.Text);

            // 4. Success
            ShowMessage("Account created successfully!", Colors.Green);

            // Navigate back to login
            EmitSignal(SignalName.RegisterSuccess);
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

    private void ShowMessage(string message, Color color)
    {
        ErrorLabel.Text = message;
        ErrorLabel.Modulate = color;
    }
}
