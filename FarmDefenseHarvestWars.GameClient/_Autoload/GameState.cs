using Godot;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;

public partial class GameState : Node
{
    // Singleton
    public static GameState Instance { get; private set; } = null!;

    // Datele jucătorului
    public PlayerProfileDto? CurrentProfile { get; private set; }
    public SelectedDeckData? CurrentDeck { get; private set; }

    // Computed Property - Ești logat dacă ai un profil încărcat
    public bool IsLoggedIn => CurrentProfile != null;

    public PlayerRole Role => NetworkBootstrap.Instance.Gameplay.MyRole;

    // Semnale pentru UI (Observer Pattern)
    [Signal] public delegate void ProfileUpdatedEventHandler();
    [Signal] public delegate void LoggedOutEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }

    // Funcție apelată când primim date noi de la server
    public void SetProfile(PlayerProfileDto profile)
    {
        CurrentProfile = profile;
        EmitSignal(SignalName.ProfileUpdated);
        GD.Print($"GameState Actualizat: {CurrentProfile.Email}, Gold: {CurrentProfile.Gold}");
    }

    public void SetCurrentDeck(SelectedDeckData deck)
    {
        CurrentDeck = deck;
    }

    public void ClearState()
    {
        CurrentProfile = null;
        CurrentDeck = null;
        EmitSignal(SignalName.LoggedOut);
    }
}
