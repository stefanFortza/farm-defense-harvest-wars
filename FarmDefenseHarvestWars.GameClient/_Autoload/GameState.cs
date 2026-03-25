using Godot;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using System.Collections.Generic;

public partial class GameState : Node
{
    // Singleton
    public static GameState Instance { get; private set; } = null!;

    // Datele jucătorului
    public PlayerProfileDto? CurrentProfile { get; private set; }
    public SelectedDeckData? CurrentDeck { get; private set; }

    // Computed Property - Ești logat dacă ai un profil încărcat
    public bool IsLoggedIn => CurrentProfile != null;

    public PlayerRole? AssignedRole { get; private set; }
    public bool HasAssignedRole => AssignedRole.HasValue;
    public bool IsDedicatedServerProcess => CmdArgs.IsServer;
    public bool IsNetworkServer => Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer();

    // Semnale pentru UI (Observer Pattern)
    [Signal] public delegate void ProfileUpdatedEventHandler();
    [Signal] public delegate void LoggedOutEventHandler();
    [Signal] public delegate void RoleAssignedEventHandler(int role);

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

    public void SetDeckForRole(PlayerRole role, IReadOnlyCollection<UnitType> units)
    {
        CurrentDeck ??= new SelectedDeckData();

        if (role == PlayerRole.Defender)
        {
            CurrentDeck.DefenderDeck = [.. units];
            return;
        }

        if (role == PlayerRole.Attacker)
        {
            CurrentDeck.AttackerDeck = [.. units];
        }
    }

    public void SetAssignedRole(PlayerRole role)
    {
        AssignedRole = role;
        EmitSignal(SignalName.RoleAssigned, (int)role);
    }

    public void ClearState()
    {
        CurrentProfile = null;
        CurrentDeck = null;
        AssignedRole = null;
        EmitSignal(SignalName.LoggedOut);
    }
}
