using System.Collections.Generic;
using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Scenes.Menus.MainMenu.MenuLabel;

public abstract partial class DeckSelectionLeft : Control
{
    [Export] protected GridContainer _slotsContainer = null!;
    [Export] protected UnitRegistry _unitRegistry = null!;
    [Export] protected PackedScene _slotScene = null!;

    protected readonly List<DeckSlotControl> _slots = new();
    protected bool _isSavingDeck;

    /// <summary>
    /// Returns the role this page is responsible for.
    /// Must be implemented by subclasses (e.g., Attacker or Defender).
    /// </summary>
    protected abstract PlayerRole GetRole();

    public override void _Ready()
    {
        this.EnsureNotNull(_slotsContainer, nameof(_slotsContainer));
        this.EnsureNotNull(_unitRegistry, nameof(_unitRegistry));
        this.EnsureNotNull(_slotScene, nameof(_slotScene));

        _unitRegistry.InitializeLookup();

        BuildSlots();
        ConnectStateSignals();
        Refresh();
    }

    public override void _ExitTree()
    {
        DisconnectStateSignals();
    }

    protected virtual void BuildSlots()
    {
        foreach (Node child in _slotsContainer.GetChildren())
        {
            child.QueueFree();
        }
        _slots.Clear();

        for (int i = 0; i < DeckService.MaxCards; i++)
        {
            DeckSlotControl? slot = _slotScene.Instantiate<DeckSlotControl>();
            if (slot == null)
            {
                GD.PrintErr("Failed to instantiate deck slot scene.");
                continue;
            }

            slot.SlotIndex = i;
            slot.SlotDropRequested += OnSlotDropRequested;
            _slotsContainer.AddChild(slot);
            _slots.Add(slot);
        }
    }

    protected virtual void ConnectStateSignals()
    {
        var state = GameState.Instance;
        if (state == null)
        {
            return;
        }

        state.DeckUpdated += OnDeckUpdated;
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

    protected virtual void OnDeckSaveStatusChanged(int roleValue, bool isSaving, bool _isSuccess, string _message)
    {
        if (roleValue != (int)GetRole())
        {
            return;
        }

        _isSavingDeck = isSaving;
    }

    protected virtual void OnSlotDropRequested(int targetIndex, int unitTypeValue, int fromSlotIndex)
    {
        var role = GetRole();
        if (!DeckService.Instance.CanEditDeckInMenu(role))
        {
            return;
        }

        if (GameState.Instance.IsDeckSaveInProgress(role))
        {
            return;
        }

        var unitType = (UnitType)unitTypeValue;
        if (!DeckService.Instance.IsRoleCompatible(_unitRegistry, unitType, role))
        {
            return;
        }

        var deck = DeckService.Instance.GetDeckForRole(role, _unitRegistry);
        var originalDeck = new List<UnitType>(deck);

        if (fromSlotIndex >= 0)
        {
            MoveDeckEntry(deck, fromSlotIndex, targetIndex);
        }
        else
        {
            InsertFromLibrary(deck, unitType, targetIndex);
        }

        if (!DecksAreEqual(originalDeck, deck))
        {
            DeckService.Instance.SubmitDeckSaveForRole(role, deck, _unitRegistry);
        }
    }

    protected virtual void MoveDeckEntry(List<UnitType> deck, int fromIndex, int targetIndex)
    {
        if (fromIndex < 0 || fromIndex >= deck.Count)
        {
            return;
        }

        int clampedTarget = Mathf.Clamp(targetIndex, 0, DeckService.MaxCards - 1);
        if (clampedTarget == fromIndex)
        {
            return;
        }

        if (clampedTarget >= deck.Count)
        {
            var moving = deck[fromIndex];
            deck.RemoveAt(fromIndex);
            deck.Add(moving);
            return;
        }

        (deck[fromIndex], deck[clampedTarget]) = (deck[clampedTarget], deck[fromIndex]);
    }

    protected static bool DecksAreEqual(List<UnitType> left, List<UnitType> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    protected virtual void InsertFromLibrary(List<UnitType> deck, UnitType unitType, int targetIndex)
    {
        if (deck.Contains(unitType))
        {
            return;
        }

        int clampedTarget = Mathf.Clamp(targetIndex, 0, DeckService.MaxCards - 1);

        if (clampedTarget >= deck.Count)
        {
            if (deck.Count < DeckService.MaxCards)
            {
                deck.Add(unitType);
            }
            return;
        }

        if (deck.Count < DeckService.MaxCards)
        {
            deck.Insert(clampedTarget, unitType);
            return;
        }

        deck[clampedTarget] = unitType;
    }

    protected virtual void Refresh()
    {
        var role = GetRole();
        _isSavingDeck = GameState.Instance?.IsDeckSaveInProgress(role) ?? false;

        var deck = DeckService.Instance.GetDeckForRole(role, _unitRegistry);
        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < deck.Count)
            {
                UnitType type = deck[i];
                var unitData = _unitRegistry.GetUnitData(type);
                _slots[i].SetUnit(unitData);
            }
            else
            {
                _slots[i].ClearUnit();
            }
        }
    }
}
