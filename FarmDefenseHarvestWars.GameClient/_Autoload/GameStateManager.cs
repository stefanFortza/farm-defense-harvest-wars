using Godot;
using System;

public partial class GameStateManager : Node
{
    public static bool IsLoggedIn { get; set; } = false;
    public static string CurrentScene { get; set; } = "";
    public static string CurrentUserEmail { get; set; } = "";

    public override void _Ready()
    {
    }
}
