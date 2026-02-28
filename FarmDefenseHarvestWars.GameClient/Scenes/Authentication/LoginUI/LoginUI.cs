using Godot;
using System;
using Refit;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;

namespace FarmDefenseHarvestWars.GameClient.Scenes.Authentication.LoginUI;

public partial class LoginUI : Control
{
    [Signal] public delegate void LoginSuccessEventHandler();

    [Export] public LineEdit EmailInput { get; set; } = null!;
    [Export] public LineEdit PasswordInput { get; set; } = null!;
    [Export] public Label ErrorLabel { get; set; } = null!;
    [Export] public Button LoginButton { get; set; } = null!;

    public override void _Ready()
    {
        // Fail fast if any export is missing
        this.EnsureNotNull(EmailInput, nameof(EmailInput));
        this.EnsureNotNull(PasswordInput, nameof(PasswordInput));
        this.EnsureNotNull(ErrorLabel, nameof(ErrorLabel));
        this.EnsureNotNull(LoginButton, nameof(LoginButton));

        ErrorLabel.Text = "";

        // Check command line arguments for auto-login
        if (!string.IsNullOrEmpty(CmdArgs.Email) && !string.IsNullOrEmpty(CmdArgs.Password))
        {
            EmailInput.Text = CmdArgs.Email;
            PasswordInput.Text = CmdArgs.Password;

            GD.Print($">>> AUTO-LOGIN DETECTED: {CmdArgs.Email} <<<");
            OnLoginPressed();
        }
    }

    public async void OnLoginPressed()
    {
        LoginButton.Disabled = true;
        ShowMessage("Connecting...", Colors.Yellow);

        // 1. Local Validation
        if (string.IsNullOrWhiteSpace(EmailInput.Text) || string.IsNullOrWhiteSpace(PasswordInput.Text))
        {
            ShowMessage("Please enter email and password!", Colors.Red);
            LoginButton.Disabled = false;
            return;
        }

        try
        {
            // 2. Network Request
            await NetworkBootstrap.Instance.Auth.LoginAsync(EmailInput.Text, PasswordInput.Text);

            ShowMessage("Success! Loading...", Colors.Green);
            EmitSignal(SignalName.LoginSuccess);
        }
        catch (ApiException ex)
        {
            ShowMessage("Invalid email or password!", Colors.Red);
            GD.PrintErr(ex.Content);
        }
        catch (Exception ex)
        {
            ShowMessage("Could not connect to server.", Colors.Red);
            GD.PrintErr(ex.Message);
        }
        finally
        {
            LoginButton?.Disabled = false;
        }
    }

    private void ShowMessage(string message, Color color)
    {
        ErrorLabel.Text = message;
        ErrorLabel.Modulate = color;
    }
}
