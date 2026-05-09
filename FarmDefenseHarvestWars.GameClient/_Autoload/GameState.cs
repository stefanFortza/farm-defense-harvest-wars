using Godot;
using FarmDefenseHarvestWars.Shared.Models.Game;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using System.Collections.Generic;
using System.Linq;
using System;

public partial class GameState : Node
{
    private static readonly IReadOnlyList<UnitUnlockDto> EmptyUnlockDeck = [];

    // Singleton
    public static GameState Instance { get; private set; } = null!;

    // Datele jucătorului
    public PlayerProfileDto? CurrentProfile { get; private set; }
    public SelectedDeckData? CurrentDeck { get; private set; }
    public string? AccessToken { get; set; }

    // Match configuration for active game (server loads it from cmd args, clients receive it via RPC)
    private string? _matchId;
    public string? MatchId 
    { 
        get => _matchId; 
        private set 
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _matchId = value;
                return;
            }

            // Defensive check: If the ID looks like a file path or namespace, it's corrupted
            if (value.Contains("/") || value.Contains(".tscn") || value.Length > 64)
            {
                GD.PrintErr($"[GameState] REJECTED corrupted MatchId: '{value}'. This looks like a scene path, not a GUID.");
                return;
            }

            if (_matchId != value)
            {
                GD.Print($"[GameState] MatchId changing from '{_matchId}' to '{value}'");
                _matchId = value;
            }
        }
    }
    public IReadOnlyList<UnitUnlockDto>? DefenderDeck { get; private set; }
    public IReadOnlyList<UnitUnlockDto>? AttackerDeck { get; private set; }
    public int DefenderAvatarIndex { get; private set; } = 1;
    public int AttackerAvatarIndex { get; private set; } = 1;
    public string DefenderName { get; private set; } = "Defender";
    public string AttackerName { get; private set; } = "Attacker";

    // Computed Property - Ești logat dacă ai un profil încărcat
    public bool IsLoggedIn => CurrentProfile != null;

    public PlayerRole? AssignedRole { get; private set; }
    public bool HasAssignedRole => AssignedRole.HasValue;
    public bool IsDedicatedServerProcess => CmdArgs.IsServer;
    public bool IsNetworkServer => Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer();
    public bool IsMatchConfigured => !string.IsNullOrEmpty(MatchId) && DefenderDeck != null && AttackerDeck != null;

    // Semnale pentru UI (Observer Pattern)
    [Signal] public delegate void ProfileUpdatedEventHandler();
    [Signal] public delegate void LoggedOutEventHandler();
    [Signal] public delegate void RoleAssignedEventHandler(int role);
    [Signal] public delegate void DeckUpdatedEventHandler(int role);
    [Signal] public delegate void DeckSaveStatusChangedEventHandler(int role, bool isSaving, bool isSuccess, string message);
    [Signal] public delegate void MatchConfigurationLoadedEventHandler();
    [Signal] public delegate void UnitUpgradedEventHandler(int unitType, int newLevel);

    private readonly HashSet<PlayerRole> _deckSavesInFlight = [];
    private readonly object _deckStateSync = new();

    public override void _Ready()
    {
        Instance = this;

        // Load match configuration from CmdArgs in server mode
        if (CmdArgs.IsServer)
        {
            MatchId = CmdArgs.MatchId;
            DefenderDeck = CmdArgs.DefenderDeck;
            AttackerDeck = CmdArgs.AttackerDeck;
            DefenderAvatarIndex = CmdArgs.DefenderAvatarIndex;
            AttackerAvatarIndex = CmdArgs.AttackerAvatarIndex;
            DefenderName = CmdArgs.DefenderName;
            AttackerName = CmdArgs.AttackerName;

            if (IsMatchConfigured)
            {
                GD.Print($"[GameState] Match configured | MatchId: {MatchId} | Defender deck: {string.Join(", ", DefenderDeck!)} | Attacker deck: {string.Join(", ", AttackerDeck!)}");
                EmitSignal(SignalName.MatchConfigurationLoaded);
            }
            else if (!string.IsNullOrEmpty(MatchId) || CmdArgs.DefenderDeck != null || CmdArgs.AttackerDeck != null)
            {
                GD.PrintErr($"[GameState] Incomplete match configuration | MatchId: {MatchId} | DefenderDeck: {(CmdArgs.DefenderDeck != null ? "set" : "null")} | AttackerDeck: {(CmdArgs.AttackerDeck != null ? "set" : "null")}");
            }
        }
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
        lock (_deckStateSync)
        {
            CurrentDeck = deck;
        }

        if (HasAssignedRole)
        {
            EmitSignal(SignalName.DeckUpdated, (int)AssignedRole!.Value);
        }
    }

    public void SetMatchDecks(string matchId, IReadOnlyList<UnitUnlockDto> defenderDeck, IReadOnlyList<UnitUnlockDto> attackerDeck)
    {
        MatchId = matchId;
        DefenderDeck = [.. defenderDeck];
        AttackerDeck = [.. attackerDeck];
        
        // If we already have the names/avatars from CmdArgs (server side), 
        // emitting here is fine. On client, we usually call SetMatchAvatars first now.
        if (IsMatchConfigured)
        {
            EmitSignal(SignalName.MatchConfigurationLoaded);
        }
    }

    public void SetMatchInfo(string matchId, 
        IReadOnlyList<UnitUnlockDto> defenderDeck, IReadOnlyList<UnitUnlockDto> attackerDeck,
        int defenderAvatar, int attackerAvatar,
        string defenderName, string attackerName)
    {
        MatchId = matchId;
        DefenderDeck = [.. defenderDeck];
        AttackerDeck = [.. attackerDeck];
        DefenderAvatarIndex = defenderAvatar;
        AttackerAvatarIndex = attackerAvatar;
        DefenderName = defenderName;
        AttackerName = attackerName;

        GD.Print($"[GameState] Match info synchronized: {DefenderName} vs {AttackerName}");
        EmitSignal(SignalName.MatchConfigurationLoaded);
    }

    public void SetMatchAvatars(int defenderAvatarIndex, int attackerAvatarIndex, string defenderName, string attackerName)
    {
        DefenderAvatarIndex = defenderAvatarIndex;
        AttackerAvatarIndex = attackerAvatarIndex;
        DefenderName = defenderName;
        AttackerName = attackerName;
    }

    public IReadOnlyList<UnitUnlockDto> GetMatchDeckForRole(PlayerRole role)
    {
        return role == PlayerRole.Defender
            ? DefenderDeck ?? EmptyUnlockDeck
            : AttackerDeck ?? EmptyUnlockDeck;
    }

    public IReadOnlyList<UnitUnlockDto> GetMyMatchDeck()
    {
        if (!AssignedRole.HasValue)
        {
            return EmptyUnlockDeck;
        }

        return GetMatchDeckForRole(AssignedRole.Value);
    }

    public bool IsUnitUnlocked(PlayerRole role, UnitType unitType)
    {
        return GetUnitUnlock(role, unitType) != null;
    }

    public UnitUnlockDto? GetUnitUnlock(PlayerRole role, UnitType unitType)
    {
        if (CurrentProfile?.UnlockedUnits == null)
        {
            return null;
        }

        if (role == PlayerRole.Any)
        {
            var defenderUnlock = CurrentProfile.UnlockedUnits.DefenderUnits.FirstOrDefault(u => u.UnitType == unitType);
            if (defenderUnlock != null) return defenderUnlock;
            
            return CurrentProfile.UnlockedUnits.AttackerUnits.FirstOrDefault(u => u.UnitType == unitType);
        }

        var list = role == PlayerRole.Defender
            ? CurrentProfile.UnlockedUnits.DefenderUnits
            : CurrentProfile.UnlockedUnits.AttackerUnits;

        return list.FirstOrDefault(u => u.UnitType == unitType);
    }

    public void SetDeckForRole(PlayerRole role, IReadOnlyCollection<UnitType> units)
    {
        bool shouldEmit = false;
        lock (_deckStateSync)
        {
            CurrentDeck ??= new SelectedDeckData();

            if (role == PlayerRole.Defender)
            {
                CurrentDeck.DefenderDeck = [.. units];
                shouldEmit = true;
            }

            if (role == PlayerRole.Attacker)
            {
                CurrentDeck.AttackerDeck = [.. units];
                shouldEmit = true;
            }
        }

        if (shouldEmit)
        {
            EmitSignal(SignalName.DeckUpdated, (int)role);
        }
    }

    public IReadOnlyList<UnitType> GetSelectedDeckForRoleSnapshot(PlayerRole role)
    {
        lock (_deckStateSync)
        {
            if (CurrentDeck == null)
            {
                return [];
            }

            return role == PlayerRole.Attacker
                ? [.. CurrentDeck.AttackerDeck]
                : [.. CurrentDeck.DefenderDeck];
        }
    }

    public bool IsDeckSaveInProgress(PlayerRole role)
    {
        lock (_deckStateSync)
        {
            return _deckSavesInFlight.Contains(role);
        }
    }

    public void SetDeckSaveInProgress(PlayerRole role, bool isSaving)
    {
        lock (_deckStateSync)
        {
            if (isSaving)
            {
                _deckSavesInFlight.Add(role);
            }
            else
            {
                _deckSavesInFlight.Remove(role);
            }
        }

        EmitSignal(SignalName.DeckSaveStatusChanged, (int)role, isSaving, true, string.Empty);
    }

    public void NotifyDeckSaveResult(PlayerRole role, bool isSuccess, string message)
    {
        lock (_deckStateSync)
        {
            _deckSavesInFlight.Remove(role);
        }

        EmitSignal(SignalName.DeckSaveStatusChanged, (int)role, false, isSuccess, message);
    }

    public void SetAssignedRole(PlayerRole role)
    {
        AssignedRole = role;
        EmitSignal(SignalName.RoleAssigned, (int)role);
    }

    public void ClearState()
    {
        lock (_deckStateSync)
        {
            CurrentProfile = null;
            CurrentDeck = null;
            MatchId = null;
            DefenderDeck = null;
            AttackerDeck = null;
            AssignedRole = null;
            _deckSavesInFlight.Clear();
        }

        EmitSignal(SignalName.LoggedOut);
    }
}
