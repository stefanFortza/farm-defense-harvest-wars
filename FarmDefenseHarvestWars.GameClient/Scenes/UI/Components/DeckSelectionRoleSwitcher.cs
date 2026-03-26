using FarmDefenseHarvestWars.GameClient.Core.Utils;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Scenes.UI.Components;

public partial class DeckSelectionRoleSwitcher : Control
{
    private enum DeckRole
    {
        Attacker,
        Defender
    }

    [Export] private TabButton _deckSelectionTab = null!;

    [Export] private Control _leftAttacker = null!;
    [Export] private Control _leftDefender = null!;
    [Export] private Control _rightAttacker = null!;
    [Export] private Control _rightDefender = null!;

    [Export] private Control _roleTabsContainer = null!;
    [Export] private Button _attackerButton = null!;
    [Export] private Button _defenderButton = null!;

    private DeckRole _selectedRole = DeckRole.Attacker;

    public override void _Ready()
    {
        this.EnsureNotNull(_deckSelectionTab, nameof(_deckSelectionTab));
        this.EnsureNotNull(_leftAttacker, nameof(_leftAttacker));
        this.EnsureNotNull(_leftDefender, nameof(_leftDefender));
        this.EnsureNotNull(_rightAttacker, nameof(_rightAttacker));
        this.EnsureNotNull(_rightDefender, nameof(_rightDefender));
        this.EnsureNotNull(_roleTabsContainer, nameof(_roleTabsContainer));
        this.EnsureNotNull(_attackerButton, nameof(_attackerButton));
        this.EnsureNotNull(_defenderButton, nameof(_defenderButton));

        _deckSelectionTab.Toggled += OnDeckSelectionTabToggled;
        _deckSelectionTab.AnimationFinished += OnDeckSelectionAnimationFinished;

        _attackerButton.Pressed += OnAttackerPressed;
        _defenderButton.Pressed += OnDefenderPressed;

        ApplyRoleButtonState();
        UpdateRoleTabsVisibility();

        if (_deckSelectionTab.ButtonPressed)
        {
            ApplyRoleVisibility();
        }
    }

    public override void _ExitTree()
    {
        if (_deckSelectionTab != null)
        {
            _deckSelectionTab.Toggled -= OnDeckSelectionTabToggled;
            _deckSelectionTab.AnimationFinished -= OnDeckSelectionAnimationFinished;
        }

        if (_attackerButton != null)
        {
            _attackerButton.Pressed -= OnAttackerPressed;
        }

        if (_defenderButton != null)
        {
            _defenderButton.Pressed -= OnDefenderPressed;
        }
    }

    private void OnDeckSelectionTabToggled(bool pressed)
    {
        UpdateRoleTabsVisibility();

        if (pressed)
        {
            ApplyRoleVisibility();
        }
    }

    private void OnDeckSelectionAnimationFinished(TabButton _, TabButton.TabButtonState finalState)
    {
        if (finalState != TabButton.TabButtonState.Active)
        {
            return;
        }

        if (_deckSelectionTab.ButtonPressed)
        {
            ApplyRoleVisibility();
        }
    }

    private void OnAttackerPressed()
    {
        _selectedRole = DeckRole.Attacker;
        ApplyRoleButtonState();

        if (_deckSelectionTab.ButtonPressed)
        {
            ApplyRoleVisibility();
        }
    }

    private void OnDefenderPressed()
    {
        _selectedRole = DeckRole.Defender;
        ApplyRoleButtonState();

        if (_deckSelectionTab.ButtonPressed)
        {
            ApplyRoleVisibility();
        }
    }

    private void UpdateRoleTabsVisibility()
    {
        _roleTabsContainer.Visible = _deckSelectionTab.ButtonPressed;
    }

    private void ApplyRoleButtonState()
    {
        bool isAttacker = _selectedRole == DeckRole.Attacker;
        _attackerButton.ButtonPressed = isAttacker;
        _defenderButton.ButtonPressed = !isAttacker;
    }

    private void ApplyRoleVisibility()
    {
        bool isAttacker = _selectedRole == DeckRole.Attacker;

        _leftAttacker.Visible = isAttacker;
        _rightAttacker.Visible = isAttacker;

        _leftDefender.Visible = !isAttacker;
        _rightDefender.Visible = !isAttacker;
    }
}
