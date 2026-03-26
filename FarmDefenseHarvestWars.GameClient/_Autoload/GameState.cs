using Godot;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using System.Collections.Generic;
using System.Linq;

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
    [Signal] public delegate void DeckUpdatedEventHandler(int role);
    [Signal] public delegate void DeckSaveStatusChangedEventHandler(int role, bool isSaving, bool isSuccess, string message);

    private readonly HashSet<PlayerRole> _deckSavesInFlight = [];

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

        if (HasAssignedRole)
        {
            EmitSignal(SignalName.DeckUpdated, (int)AssignedRole!.Value);
        }
    }

    public bool IsUnitUnlocked(PlayerRole role, UnitType unitType)
    {
        if (CurrentProfile?.UnlockedUnits == null)
        {
            return false;
        }

        return role switch
        {
            PlayerRole.Defender => CurrentProfile.UnlockedUnits.DefenderUnits.Contains(unitType),
            PlayerRole.Attacker => CurrentProfile.UnlockedUnits.AttackerUnits.Contains(unitType),
            _ => false
        };
    }

    public void SetDeckForRole(PlayerRole role, IReadOnlyCollection<UnitType> units)
    {
        CurrentDeck ??= new SelectedDeckData();

        if (role == PlayerRole.Defender)
        {
            CurrentDeck.DefenderDeck = [.. units];
            EmitSignal(SignalName.DeckUpdated, (int)role);
            return;
        }

        if (role == PlayerRole.Attacker)
        {
            CurrentDeck.AttackerDeck = [.. units];
            EmitSignal(SignalName.DeckUpdated, (int)role);
        }
    }

    public bool IsDeckSaveInProgress(PlayerRole role)
    {
        return _deckSavesInFlight.Contains(role);
    }

    public void SetDeckSaveInProgress(PlayerRole role, bool isSaving)
    {
        if (isSaving)
        {
            _deckSavesInFlight.Add(role);
        }
        else
        {
            _deckSavesInFlight.Remove(role);
        }

        EmitSignal(SignalName.DeckSaveStatusChanged, (int)role, isSaving, true, string.Empty);
    }

    public void NotifyDeckSaveResult(PlayerRole role, bool isSuccess, string message)
    {
        EmitSignal(SignalName.DeckSaveStatusChanged, (int)role, false, isSuccess, message);
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
        _deckSavesInFlight.Clear();
        EmitSignal(SignalName.LoggedOut);
    }
}
