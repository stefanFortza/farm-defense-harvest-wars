using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using Refit;

public abstract partial class DeckSelectionRight : Control
{
    [Export] protected Label _titleLabel = null!;
    [Export] protected Label _statusLabel = null!;
    [Export] protected GridContainer _libraryContainer = null!;
    [Export] protected UnitRegistry _unitRegistry = null!;
    [Export] protected PackedScene _libraryItemScene = null!;

    protected readonly List<DeckLibraryItemControl> _libraryItems = new();
    private readonly HashSet<UnitType> _unlockInFlight = [];
    private bool _isSavingDeck;
    private int _statusMessageVersion;

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
    }

    public override void _ExitTree()
    {
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
            _statusLabel.Text = "Saving deck...";
            _statusLabel.Modulate = Colors.Goldenrod;
            return;
        }

        if (string.IsNullOrWhiteSpace(message) && isSuccess)
        {
            ShowTransientStatus("Deck saved", Colors.LightGreen, 0.9);
            return;
        }

        if (!isSuccess)
        {
            ShowTransientStatus($"Save failed: {message}", Colors.IndianRed, 2.2);
        }
    }

    protected virtual void OnProfileUpdated()
    {
        Refresh();
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (_isSavingDeck)
        {
            return false;
        }

        if (data.Obj is not Dictionary<string, Variant> dict)
        {
            return false;
        }

        // Only accept drops from deck slots (fromSlot >= 0 indicates it's from left page)
        return dict.ContainsKey("fromSlot");
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (_isSavingDeck)
        {
            return;
        }

        if (data.Obj is not Dictionary<string, Variant> dict)
        {
            return;
        }

        if (!dict.TryGetValue("fromSlot", out var fromSlotObj) || fromSlotObj.VariantType != Variant.Type.Int)
        {
            return;
        }

        int fromSlot = (int)fromSlotObj;

        if (fromSlot < 0)
        {
            return; // Not from a deck slot
        }

        // Get the role and the deck
        var role = GetRole();
        var deck = DeckSelectionLogic.GetDeckForRole(role);

        // Remove the unit at fromSlot
        if (fromSlot < deck.Count)
        {
            deck.RemoveAt(fromSlot);
            DeckSelectionLogic.SubmitDeckSaveForRole(role, deck);
        }

        GetTree().Root.SetInputAsHandled();
    }

    protected virtual void Refresh()
    {
        var role = GetRole();
        _titleLabel.Text = $"Library ({role})";

        // Clear existing items
        foreach (Node child in _libraryContainer.GetChildren())
        {
            child.QueueFree();
        }
        _libraryItems.Clear();

        // Get compatible units for this role
        var compatible = DeckSelectionLogic.GetCompatibleUnits(_unitRegistry, role);
        var currentDeck = DeckSelectionLogic.GetDeckForRole(role);
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
}
