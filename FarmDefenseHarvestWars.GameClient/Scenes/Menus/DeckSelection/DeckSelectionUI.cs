using Godot;
using Godot.Collections;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Core.Utils;

public partial class DeckSelectionUI : Control
{
    private const int MaxCards = 5;

    [Export] private Label _titleLabel = null!;
    [Export] private VBoxContainer _cardList = null!;
    [Export] private Label _counterLabel = null!;
    [Export] private Button _confirmButton = null!;
    [Export] private UnitRegistry _unitRegistry = null!;

    private readonly Array<UnitType> _selectedUnits = [];

    public override void _Ready()
    {
        this.EnsureNotNull(_titleLabel, nameof(_titleLabel));
        this.EnsureNotNull(_cardList, nameof(_cardList));
        this.EnsureNotNull(_counterLabel, nameof(_counterLabel));
        this.EnsureNotNull(_confirmButton, nameof(_confirmButton));
        this.EnsureNotNull(_unitRegistry, nameof(_unitRegistry));

        _unitRegistry.InitializeLookup();
        _confirmButton.Pressed += OnConfirmPressed;

        BuildUnitList();
        RefreshFooter();
    }

    public override void _ExitTree()
    {
        _confirmButton.Pressed -= OnConfirmPressed;
    }

    private void BuildUnitList()
    {
        foreach (Node child in _cardList.GetChildren())
        {
            child.QueueFree();
        }

        PlayerRole role = GameState.Instance?.Role ?? PlayerRole.Spectator;
        _titleLabel.Text = $"Selecteaza Deck ({role})";

        foreach (var unit in _unitRegistry.AllUnits)
        {
            if (unit == null)
            {
                continue;
            }

            if (!IsRoleCompatible(unit.Type, role))
            {
                continue;
            }

            var checkBox = new CheckBox
            {
                Text = $"{unit.Name} - Cost {unit.MatchCost}",
                TooltipText = unit.Name
            };

            UnitType currentType = unit.Type;
            checkBox.Toggled += (pressed) => OnUnitToggled(currentType, pressed, checkBox);
            _cardList.AddChild(checkBox);
        }
    }

    private void OnUnitToggled(UnitType unitType, bool pressed, CheckBox checkBox)
    {
        if (pressed)
        {
            if (_selectedUnits.Count >= MaxCards)
            {
                checkBox.ButtonPressed = false;
                return;
            }

            if (!_selectedUnits.Contains(unitType))
            {
                _selectedUnits.Add(unitType);
            }
        }
        else
        {
            _selectedUnits.Remove(unitType);
        }

        RefreshFooter();
    }

    private void RefreshFooter()
    {
        _counterLabel.Text = $"Selected: {_selectedUnits.Count}/{MaxCards}";
        _confirmButton.Disabled = _selectedUnits.Count == 0;
    }

    private void OnConfirmPressed()
    {
        PlayerRole role = GameState.Instance?.Role ?? PlayerRole.Spectator;

        var deck = GameState.Instance?.CurrentDeck ?? new SelectedDeckData();

        if (role == PlayerRole.Attacker)
        {
            deck.AttackerDeck = [.. _selectedUnits];
            if (deck.DefenderDeck.Count == 0)
            {
                deck.DefenderDeck = BuildDefaultDeck(PlayerRole.Defender);
            }
        }
        else
        {
            deck.DefenderDeck = [.. _selectedUnits];
            if (deck.AttackerDeck.Count == 0)
            {
                deck.AttackerDeck = BuildDefaultDeck(PlayerRole.Attacker);
            }
        }

        GameState.Instance?.SetCurrentDeck(deck);
        GetTree().ChangeSceneToFile("res://Scenes/Gameplay/GameWorld/GameWorld.tscn");
    }

    private Array<UnitType> BuildDefaultDeck(PlayerRole role)
    {
        var deck = new Array<UnitType>();

        foreach (var unit in _unitRegistry.AllUnits)
        {
            if (unit == null)
            {
                continue;
            }

            if (!IsRoleCompatible(unit.Type, role))
            {
                continue;
            }

            deck.Add(unit.Type);
            if (deck.Count >= MaxCards)
            {
                break;
            }
        }

        return deck;
    }

    private static bool IsRoleCompatible(UnitType unitType, PlayerRole role)
    {
        if (role == PlayerRole.Attacker)
        {
            return unitType == UnitType.Skeleton;
        }

        if (role == PlayerRole.Defender)
        {
            return unitType != UnitType.Skeleton;
        }

        return true;
    }
}
