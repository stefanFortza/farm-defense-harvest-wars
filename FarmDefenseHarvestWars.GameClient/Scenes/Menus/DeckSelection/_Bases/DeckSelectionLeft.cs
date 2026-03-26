using System.Collections.Generic;
using Godot;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;

public abstract partial class DeckSelectionLeft : Control
{
    [Export] protected Label _titleLabel = null!;
    [Export] protected VBoxContainer _slotsContainer = null!;
    [Export] protected UnitRegistry _unitRegistry = null!;
    [Export] protected PackedScene _slotScene = null!;

    protected readonly List<DeckSlotControl> _slots = new();

    /// <summary>
    /// Returns the role this page is responsible for.
    /// Must be implemented by subclasses (e.g., Attacker or Defender).
    /// </summary>
    protected abstract PlayerRole GetRole();

    public override void _Ready()
    {
        this.EnsureNotNull(_titleLabel, nameof(_titleLabel));
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

        for (int i = 0; i < DeckSelectionLogic.MaxCards; i++)
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
    }

    protected virtual void DisconnectStateSignals()
    {
        var state = GameState.Instance;
        if (state == null)
        {
            return;
        }

        state.DeckUpdated -= OnDeckUpdated;
    }

    protected virtual void OnDeckUpdated(int _roleValue)
    {
        Refresh();
    }

    protected virtual void OnSlotDropRequested(int targetIndex, int unitTypeValue, int fromSlotIndex)
    {
        // Guard: deck editing only allowed in menu (no assigned role)
        if (GameState.Instance.HasAssignedRole)
        {
            return;
        }

        var role = GetRole();
        var unitType = (UnitType)unitTypeValue;
        if (!DeckSelectionLogic.IsRoleCompatible(unitType, role))
        {
            return;
        }

        var deck = DeckSelectionLogic.GetDeckForRole(role);

        if (fromSlotIndex >= 0)
        {
            MoveDeckEntry(deck, fromSlotIndex, targetIndex);
        }
        else
        {
            InsertFromLibrary(deck, unitType, targetIndex);
        }

        _ = DeckSelectionLogic.SaveDeckForRole(role, deck);
    }

    protected virtual void MoveDeckEntry(List<UnitType> deck, int fromIndex, int targetIndex)
    {
        if (fromIndex < 0 || fromIndex >= deck.Count)
        {
            return;
        }

        var moving = deck[fromIndex];
        deck.RemoveAt(fromIndex);

        int insertIndex = Mathf.Clamp(targetIndex, 0, deck.Count);
        if (fromIndex < targetIndex)
        {
            insertIndex = Mathf.Max(0, insertIndex - 1);
        }

        if (insertIndex >= deck.Count)
        {
            deck.Add(moving);
            return;
        }

        deck.Insert(insertIndex, moving);
    }

    protected virtual void InsertFromLibrary(List<UnitType> deck, UnitType unitType, int targetIndex)
    {
        if (deck.Contains(unitType))
        {
            return;
        }

        int clampedTarget = Mathf.Clamp(targetIndex, 0, DeckSelectionLogic.MaxCards - 1);

        if (clampedTarget >= deck.Count)
        {
            if (deck.Count < DeckSelectionLogic.MaxCards)
            {
                deck.Add(unitType);
            }
            return;
        }

        if (deck.Count < DeckSelectionLogic.MaxCards)
        {
            deck.Insert(clampedTarget, unitType);
            return;
        }

        deck[clampedTarget] = unitType;
    }

    protected virtual void Refresh()
    {
        var role = GetRole();
        _titleLabel.Text = $"Deck ({role})";

        var deck = DeckSelectionLogic.GetDeckForRole(role);
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
