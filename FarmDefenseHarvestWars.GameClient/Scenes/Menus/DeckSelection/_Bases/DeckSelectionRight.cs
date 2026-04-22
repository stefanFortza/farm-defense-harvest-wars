using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using Refit;

public abstract partial class DeckSelectionRight : Control
{
    private const double DeckSyncPollSeconds = 20.0;

    [Export] protected Label _titleLabel = null!;
    [Export] protected Label _statusLabel = null!;
    [Export] protected GridContainer _libraryContainer = null!;
    [Export] protected UnitRegistry _unitRegistry = null!;
    [Export] protected PackedScene _libraryItemScene = null!;

    protected readonly List<DeckLibraryItemControl> _libraryItems = new();
    private readonly HashSet<UnitType> _unlockInFlight = [];
    private bool _isSavingDeck;
    private int _statusMessageVersion;
    private string _saveLoadingToastId = string.Empty;
    private CancellationTokenSource? _deckSyncCts;
    private readonly SemaphoreSlim _deckSyncGate = new(1, 1);

    /// <summary>
    /// Returns the role this page is responsible for.
    /// Must be implemented by subclasses (e.g., Attacker or Defender).
    /// </summary>
    protected abstract PlayerRole GetRole();

    public override void _Ready()
    {
        this.EnsureNotNull(_titleLabel, nameof(_titleLabel));
        this.EnsureNotNull(_statusLabel, nameof(_statusLabel));
        this.EnsureNotNull(_libraryContainer, nameof(_libraryContainer));
        this.EnsureNotNull(_unitRegistry, nameof(_unitRegistry));
        this.EnsureNotNull(_libraryItemScene, nameof(_libraryItemScene));

        _statusLabel.Text = string.Empty;

        _unitRegistry.InitializeLookup();

        ConnectStateSignals();
        Refresh();
        StartDeckSyncLoop();
    }

    public override void _ExitTree()
    {
        DismissSaveLoadingToast();
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
            if (string.IsNullOrWhiteSpace(_saveLoadingToastId)
                && ToastNotifications.TryShowLoading("Saving deck...", out var toastId))
            {
                _saveLoadingToastId = toastId;
                _statusLabel.Text = string.Empty;
                _statusLabel.Modulate = Colors.White;
                return;
            }

            _statusLabel.Text = "Saving deck...";
            _statusLabel.Modulate = Colors.Goldenrod;
            return;
        }

        DismissSaveLoadingToast();

        if (string.IsNullOrWhiteSpace(message) && isSuccess)
        {
            if (!ToastNotifications.TrySuccess("Deck saved", 0.9))
            {
                ShowTransientStatus("Deck saved", Colors.LightGreen, 0.9);
            }
        }
        else if (!isSuccess)
        {
            string error = string.IsNullOrWhiteSpace(message) ? "Unknown error" : message;
            string text = $"Save failed: {error}";
            if (!ToastNotifications.TryError(text, 2.2))
            {
                ShowTransientStatus(text, Colors.IndianRed, 2.2);
            }
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

        if (GameState.Instance?.IsDeckSaveInProgress(role) == true)
        {
            return false;
        }

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

        if (GameState.Instance?.IsDeckSaveInProgress(role) == true)
        {
            return;
        }

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
        _titleLabel.Text = $"Library ({role})";
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
            bool isUnlocked = state?.IsUnitUnlocked(role, unitData.Type) ?? false;
            bool isUnlocking = _unlockInFlight.Contains(unitData.Type);
            DeckLibraryItemControl? item = _libraryItemScene.Instantiate<DeckLibraryItemControl>();
            if (item == null)
            {
                GD.PrintErr("Failed to instantiate deck library item scene.");
                continue;
            }

            item.Setup(unitData, alreadyInDeck, isUnlocked, isUnlocking, _isSavingDeck);
            _libraryContainer.AddChild(item);
            _libraryItems.Add(item);
        }
    }

    private async void ShowTransientStatus(string text, Color color, double seconds)
    {
        _statusMessageVersion++;
        int version = _statusMessageVersion;

        _statusLabel.Text = text;
        _statusLabel.Modulate = color;

        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        if (!IsInsideTree())
        {
            return;
        }

        if (version != _statusMessageVersion)
        {
            return;
        }

        _statusLabel.Text = string.Empty;
        _statusLabel.Modulate = Colors.White;
    }

    private void DismissSaveLoadingToast()
    {
        if (string.IsNullOrWhiteSpace(_saveLoadingToastId))
        {
            return;
        }

        ToastNotifications.TryDismiss(_saveLoadingToastId);
        _saveLoadingToastId = string.Empty;
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
                if (!ToastNotifications.TryInfo("Deck resynced from server", 1.6))
                {
                    ShowTransientStatus("Deck resynced from server", Colors.LightSkyBlue, 1.6);
                }
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
