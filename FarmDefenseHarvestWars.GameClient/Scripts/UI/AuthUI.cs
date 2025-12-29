using Godot;
using System;

public partial class AuthUI : Control
{
    [Export] public LoginUI LoginPanel { get; set; } = null!;
    [Export] public RegisterUI RegisterPanel { get; set; } = null!;

    public override void _Ready()
    {
        // Connect signals
        if (LoginPanel != null)
        {
            LoginPanel.LoginSuccess += OnLoginSuccess;
            LoginPanel.GoToRegister += ShowRegister;
        }

        if (RegisterPanel != null)
        {
            RegisterPanel.RegisterSuccess += OnLoginSuccess;
            RegisterPanel.BackToLogin += ShowLogin;
        }

        // Ensure correct initial state
        ShowLogin();
    }

    public void ShowLogin()
    {
        if (LoginPanel != null) LoginPanel.Visible = true;
        if (RegisterPanel != null) RegisterPanel.Visible = false;
    }

    public void ShowRegister()
    {
        if (LoginPanel != null) LoginPanel.Visible = false;
        if (RegisterPanel != null) RegisterPanel.Visible = true;
    }

    // Placeholder for actual auth logic
    public void OnLoginSuccess()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Menus/MainMenu.tscn");
    }
}
