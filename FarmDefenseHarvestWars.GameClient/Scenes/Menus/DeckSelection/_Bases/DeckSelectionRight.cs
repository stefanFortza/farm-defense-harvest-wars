using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MenuLabel;
using Refit;

public abstract partial class DeckSelectionRight : Control
{
    private const double DeckSyncPollSeconds = 20.0;

    [Export] protected GridContainer _libraryContainer = null!;
    [Export] protected UnitRegistry _unitRegistry = null!;
    [Export] protected PackedScene _libraryItemScene = null!;

    protected readonly List<DeckLibraryItemControl> _libraryItems = new();
    private readonly HashSet<UnitType> _unlockInFlight = [];
    private bool _isSavingDeck;
    private CancellationTokenSource? _deckSyncCts;
    private readonly SemaphoreSlim _deckSyncGate = new(1, 1);

    /// <summary>
    /// Returns the role this page is responsible for.
    /// Must be implemented by subclasses (e.g., Attacker or Defender).
    /// </summary>
    protected abstract PlayerRole GetRole();

    public override void _Ready()
    {
        this.EnsureNotNull(_libraryContainer, nameof(_libraryContainer));
        this.EnsureNotNull(_unitRegistry, nameof(_unitRegistry));
        this.EnsureNotNull(_libraryItemScene, nameof(_libraryItemScene));

        _unitRegistry.InitializeLookup();

        ConnectStateSignals();
        Refresh();
        StartDeckSyncLoop();
    }

    public override void _ExitTree()
    {
        StopDeckSyncLoop();
        DisconnectStateSignals();
    }

    protected virtual void ConnectStateSignals()
    {
        var state = GameState.Instance;
        if (state == null)
        {
            return;
        }

        state.DeckUpdated += OnDeckUpdated;
        state.ProfileUpdated += OnProfileUpdated;
        state.DeckSaveStatusChanged += OnDeckSaveStatusChanged;

        _isSavingDeck = state.IsDeckSaveInProgress(GetRole());
    }

    protected virtual void DisconnectStateSignals()
    {
        var state = GameState.Instance;
        if (state == null)
        {
            return;
        }

        state.DeckUpdated -= OnDeckUpdated;
        state.ProfileUpdated -= OnProfileUpdated;
        state.DeckSaveStatusChanged -= OnDeckSaveStatusChanged;
    }

    protected virtual void OnDeckUpdated(int roleValue)
    {
        if (roleValue != (int)GetRole())
        {
            return;
        }

        Refresh();
    }

    protected virtual void OnDeckSaveStatusChanged(int roleValue, bool isSaving, bool isSuccess, string message)
    {
        if (roleValue != (int)GetRole())
        {
            return;
        }

        _isSavingDeck = isSaving;

        if (isSaving)
        {
            return;
        }


        if (string.IsNullOrWhiteSpace(message) && isSuccess)
        {
            ToastNotifications.TrySuccess("Deck saved", 0.9);
        }
        else if (!isSuccess)
        {
            string error = string.IsNullOrWhiteSpace(message) ? "Unknown error" : message;
            string text = $"Save failed: {error}";
            ToastNotifications.TryError(text, 2.2);
        }

        Refresh();
    }

    protected virtual void OnProfileUpdated()
    {
        Refresh();
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var role = GetRole();
        if (!DeckService.Instance.CanEditDeckInMenu(role))
        {
            return false;
        }

        // if (GameState.Instance?.IsDeckSaveInProgress(role) == true)
        // {
        //     return false;
        // }

        if (data.VariantType != Variant.Type.Dictionary)
        {
            return false;
        }

        var dict = data.AsGodotDictionary();

        // Only accept drops from deck slots (fromSlot >= 0 indicates it's from left page)
        return dict.ContainsKey("fromSlot");
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var role = GetRole();
        if (!DeckService.Instance.CanEditDeckInMenu(role))
        {
            return;
        }

        // if (GameState.Instance?.IsDeckSaveInProgress(role) == true)
        // {
        //     return;
        // }

        if (data.VariantType != Variant.Type.Dictionary)
        {
            return;
        }

        var dict = data.AsGodotDictionary();

        if (!dict.ContainsKey("fromSlot"))
        {
            return;
        }

        int fromSlot = ReadInt(dict, "fromSlot", -1);

        if (fromSlot < 0)
        {
            return; // Not from a deck slot
        }

        // Get the role and the deck
        var deck = DeckService.Instance.GetDeckForRole(role, _unitRegistry);

        // Remove the unit at fromSlot
        if (fromSlot < deck.Count)
        {
            deck.RemoveAt(fromSlot);
            DeckService.Instance.SubmitDeckSaveForRole(role, deck, _unitRegistry);
        }

        GetTree().Root.SetInputAsHandled();
    }

    private static int ReadInt(Godot.Collections.Dictionary dict, string key, int fallback)
    {
        if (!dict.ContainsKey(key))
        {
            return fallback;
        }

        var value = dict[key];
        if (value.VariantType == Variant.Type.Int)
        {
            return (int)(long)value;
        }

        return fallback;
    }

    protected virtual void Refresh()
    {
        var role = GetRole();
        _isSavingDeck = GameState.Instance?.IsDeckSaveInProgress(role) ?? false;

        // Clear existing items
        foreach (Node child in _libraryContainer.GetChildren())
        {
            child.QueueFree();
        }
        _libraryItems.Clear();

        // Get compatible units for this role
        var compatible = DeckService.Instance.GetCompatibleUnits(_unitRegistry, role);
        var currentDeck = DeckService.Instance.GetDeckForRole(role, _unitRegistry);
        var state = GameState.Instance;

        // Create library items
        foreach (var unitData in compatible)
        {
            bool alreadyInDeck = currentDeck.Contains(unitData.Type);
            bool isUnlocked = unitData.IsDefaultUnlocked || (state?.IsUnitUnlocked(role, unitData.Type) ?? false);
            bool isUnlocking = _unlockInFlight.Contains(unitData.Type);
            DeckLibraryItemControl? item = _libraryItemScene.Instantiate<DeckLibraryItemControl>();
            if (item == null)
            {
                GD.PrintErr("Failed to instantiate deck library item scene.");
                continue;
            }

            item.Setup(unitData, alreadyInDeck, isUnlocked, isUnlocking, _isSavingDeck, role);
            item.UnlockRequested += OnUnlockRequested;
            _libraryContainer.AddChild(item);
            _libraryItems.Add(item);
        }
    }

    private async void OnUnlockRequested(int unitTypeValue)
    {
        var unitType = (UnitType)unitTypeValue;
        if (_unlockInFlight.Contains(unitType))
        {
            return;
        }

        var unitData = _unitRegistry.GetUnitData(unitType);
        if (unitData == null)
        {
            return;
        }

        var profile = GameState.Instance?.CurrentProfile;
        if (profile == null)
        {
            return;
        }

        if (profile.Gold < unitData.UnlockCost)
        {
            ToastNotifications.TryError($"Not enough gold to unlock {unitData.Name}!", 2.0);
            return;
        }

        _unlockInFlight.Add(unitType);
        Refresh();

        try
        {
            await NetworkBootstrap.Instance.Menu.UnlockUnitAsync(unitType);
            ToastNotifications.TrySuccess($"{unitData.Name} unlocked!", 1.5);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to unlock {unitType}: {ex.Message}");
            ToastNotifications.TryError($"Failed to unlock {unitData.Name}.", 2.5);
        }
        finally
        {
            _unlockInFlight.Remove(unitType);
            Refresh();
        }
    }

    private void StartDeckSyncLoop()
    {
        _deckSyncCts?.Cancel();
        _deckSyncCts?.Dispose();
        _deckSyncCts = new CancellationTokenSource();
        _ = RunDeckSyncLoopAsync(_deckSyncCts.Token);
    }

    private void StopDeckSyncLoop()
    {
        _deckSyncCts?.Cancel();
        _deckSyncCts?.Dispose();
        _deckSyncCts = null;
    }

    private async Task RunDeckSyncLoopAsync(CancellationToken token)
    {
        try
        {
            await TrySyncDeckFromServerAsync(showResyncStatus: false, token);

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(DeckSyncPollSeconds), token);
                await TrySyncDeckFromServerAsync(showResyncStatus: true, token);
            }
        }
        catch (OperationCanceledException)
        {
            // no-op
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Deck sync loop failed for {GetRole()}: {ex.Message}");
        }
    }

    private async Task TrySyncDeckFromServerAsync(bool showResyncStatus, CancellationToken token)
    {
        if (!IsInsideTree() || !IsVisibleInTree())
        {
            return;
        }

        var role = GetRole();
        if (!DeckService.Instance.CanEditDeckInMenu(role))
        {
            return;
        }

        if (!await _deckSyncGate.WaitAsync(0, token))
        {
            return;
        }

        try
        {
            bool changed = await DeckService.Instance.SyncDeckForRoleFromServerAsync(role, _unitRegistry, skipIfSaveInFlight: true);
            if (changed && showResyncStatus)
            {
                ToastNotifications.TryInfo("Deck resynced from server", 1.6);
            }
        }
        catch (ApiException ex)
        {
            GD.PrintErr($"Deck sync failed for role {role}: {ex.Message}");
        }
        finally
        {
            _deckSyncGate.Release();
        }
    }
}
