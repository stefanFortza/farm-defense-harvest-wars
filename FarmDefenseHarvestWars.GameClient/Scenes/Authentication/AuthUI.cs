using Godot;
using System;
using System.Linq;

public partial class AuthUI : Control
{
    [Export] public LoginUI LoginPanel { get; set; } = null!;
    [Export] public RegisterUI RegisterPanel { get; set; } = null!;

    public override void _Ready()
    {
        // Check for server mode to bypass auth
        if (OS.GetCmdlineArgs().Contains("--server"))
        {
            // Use CallDeferred to avoid "Parent node is busy" error during _Ready
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://Scenes/Menus/MainMenu.tscn");
            return;
        }

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
