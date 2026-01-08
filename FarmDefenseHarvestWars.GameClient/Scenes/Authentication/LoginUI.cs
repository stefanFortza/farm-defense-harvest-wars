using Godot;
using System;

public partial class LoginUI : Control
{
    [Signal] public delegate void LoginSuccessEventHandler();

    [Export] public LineEdit EmailInput { get; set; } = null!;
    [Export] public LineEdit PasswordInput { get; set; } = null!;
    [Export] public Label ErrorLabel { get; set; } = null!;
    [Export] public Button LoginButton { get; set; } = null!;

    public override void _Ready()
    {
        // Pre-fill for development convenience
        EmailInput?.Text = "fermier@joc.com";
        PasswordInput?.Text = "Password123!";
        ErrorLabel?.Text = "";
    }

    public async void OnLoginPressed()
    {
        LoginButton?.Disabled = true;
        ShowMessage("Connecting...", Colors.Yellow);

        // 1. Local Validation
        if (string.IsNullOrWhiteSpace(EmailInput.Text) || string.IsNullOrWhiteSpace(PasswordInput.Text))
        {
            ShowMessage("Please enter email and password!", Colors.Red);
            if (LoginButton != null) LoginButton.Disabled = false;
            return;
        }

        try
        {
            // 2. Network Request
            bool success = await NetworkManager.Instance.AuthenticateAsync(EmailInput.Text, PasswordInput.Text);

            if (success)
            {
                ShowMessage("Success! Loading...", Colors.Green);
                await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
                EmitSignal(SignalName.LoginSuccess);
            }
            else
            {
                ShowMessage("Invalid email or password!", Colors.Red);
            }
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
        if (ErrorLabel != null)
        {
            ErrorLabel.Text = message;
            ErrorLabel.Modulate = color;
        }
    }
}
