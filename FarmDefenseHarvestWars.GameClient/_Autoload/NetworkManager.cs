using Godot;
using System;
using NetHttp = System.Net.Http;

public partial class NetworkManager : Node
{
    // Centralized HttpClient and token storage for the game
    private static readonly NetHttp.HttpClient _http = new NetHttp.HttpClient();
    public static NetHttp.HttpClient HttpClient => _http;

    public static string? AccessToken { get; set; }

    public override void _Ready()
    {
        // Configure defaults if needed
    }
}
