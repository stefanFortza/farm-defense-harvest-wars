using Godot;
using Godot.Collections;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;

public partial class GameHUD : CanvasLayer, IInitializable<GameHudContext>
{
    [Export] private ResourcePanel _resourcePanel = null!;
    [Export] private HBoxContainer _deckContainer = null!;
    [Export] private ProgressBar _timerBar = null!;
    [Export] private TextureButton _pauseButton = null!;
    [Export] private PackedScene _cardScene = null!;

    private MatchManager _matchManager = null!;
    private InputController _inputController = null!;
    private UnitRegistry _unitRegistry = null!;

    private readonly Array<Card> _cards = [];
    private readonly Dictionary<UnitType, Card> _cardsByType = [];
    private long _localPeerId;
    private int _localMoney;
    private float _matchDuration;
    private bool _isReady;
    private bool _isBound;

    public bool IsInitialized { get; private set; } = false;


    public void Initialize(GameHudContext data)
    {
        if (IsInitialized)
        {
            GD.PrintErr("GameHUD: already initialized");
            return;
        }

        this.EnsureNotNull(data.Match, nameof(data.Match));
        this.EnsureNotNull(data.Input, nameof(data.Input));
        this.EnsureNotNull(data.UnitRegistry, nameof(data.UnitRegistry));

        _matchManager = data.Match;
        _inputController = data.Input;
        _unitRegistry = data.UnitRegistry;


        IsInitialized = true;
    }

    public override void _Ready()
    {
        this.EnsureNotNull(_resourcePanel, nameof(_resourcePanel));
        this.EnsureNotNull(_deckContainer, nameof(_deckContainer));
        this.EnsureNotNull(_timerBar, nameof(_timerBar));
        this.EnsureNotNull(_pauseButton, nameof(_pauseButton));

        _localPeerId = Multiplayer.GetUniqueId();
        _isReady = true;

        if (IsInitialized)
        {
            ApplyInitialization();
        }
    }

    public override void _ExitTree()
    {
        _matchManager.MoneyChanged -= OnMoneyChanged;
        _matchManager.TimerUpdated -= OnTimerUpdated;

        _inputController.PlacementResolved -= OnPlacementResolved;

        _pauseButton.Pressed -= OnPausePressed;
    }

    private void ApplyInitialization()
    {
        if (_isBound)
        {
            return;
        }

        _unitRegistry.InitializeLookup();
        BindGameplaySignals();
        PopulateDeck();
        RefreshAffordability();
        _resourcePanel?.UpdateDisplay(_localMoney);

        _isBound = true;
    }

    private void BindGameplaySignals()
    {
        _matchManager.MoneyChanged += OnMoneyChanged;
        _matchManager.TimerUpdated += OnTimerUpdated;
        _matchDuration = Mathf.Max(_matchManager.MatchDurationSeconds, 1f);

        _timerBar.MinValue = 0f;
        _timerBar.MaxValue = _matchDuration;
        _timerBar.Value = 0f;

        _matchManager.RequestFullSync();

        _inputController.PlacementResolved += OnPlacementResolved;

        if (GameState.Instance?.CurrentProfile != null)
        {
            _localMoney = GameState.Instance.CurrentProfile.Gold;
        }

        _pauseButton.Pressed += OnPausePressed;
    }

    private void OnPausePressed()
    {
        GetTree().Paused = !GetTree().Paused;
    }

    private void OnMoneyChanged(long peerId, int newAmount)
    {
        if (peerId != _localPeerId)
        {
            return;
        }

        _localMoney = newAmount;
        _resourcePanel?.UpdateDisplay(_localMoney);
        RefreshAffordability();
    }

    private void OnTimerUpdated(float timeRemaining)
    {

        float elapsed = Mathf.Clamp(_matchDuration - timeRemaining, 0f, _matchDuration);
        _timerBar.Value = elapsed;
    }

    private void PopulateDeck()
    {
        foreach (Node child in _deckContainer.GetChildren())
        {
            child.QueueFree();
        }
        _cards.Clear();
        _cardsByType.Clear();

        foreach (var unitData in BuildDeckDataForRole())
        {
            if (unitData == null)
            {
                continue;
            }

            var card = _cardScene.Instantiate<Card>();
            card.Setup(unitData);
            card.CardPressed += OnCardPressed;

            _deckContainer.AddChild(card);
            _cards.Add(card);
            _cardsByType[unitData.Type] = card;
        }
    }

    private Array<UnitData> BuildDeckDataForRole()
    {
        var result = new Array<UnitData>();
        var state = GameState.Instance;
        if (state == null || !state.HasAssignedRole)
        {
            return result;
        }

        PlayerRole role = state.AssignedRole!.Value;

        var selectedDeck = state.CurrentDeck;
        if (selectedDeck != null)
        {
            Array<UnitType> selectedUnits = role == PlayerRole.Attacker
                ? selectedDeck.AttackerDeck
                : selectedDeck.DefenderDeck;

            foreach (var unitType in selectedUnits)
            {
                result.Add(_unitRegistry.GetUnitData(unitType));
            }

            if (result.Count > 0)
            {
                return result;
            }
        }

        foreach (var unit in _unitRegistry.AllUnits)
        {
            if (unit == null)
            {
                continue;
            }

            if (IsRoleCompatible(unit.Type, role))
            {
                result.Add(unit);
            }
        }

        return result;
    }

    private static bool IsRoleCompatible(UnitType unitType, PlayerRole role)
    {
        if (role == PlayerRole.Attacker)
        {
            return unitType == UnitType.Skeleton;
        }

        return unitType != UnitType.Skeleton;
    }

    private void OnCardPressed(int unitTypeValue)
    {
        if (_inputController == null)
        {
            return;
        }

        UnitType unitType = (UnitType)unitTypeValue;
        _inputController.StartPlacingUnit(unitType);
    }

    private void OnPlacementResolved(long _requestId, int unitTypeValue, bool success, string reason)
    {
        if (!success)
        {
            GD.Print($"HUD: placement rejected ({reason})");
            return;
        }

        UnitType unitType = (UnitType)unitTypeValue;
        if (_cardsByType.TryGetValue(unitType, out var card))
        {
            card.StartCooldown();
        }
    }

    private void RefreshAffordability()
    {
        foreach (var card in _cards)
        {
            card.SetAffordable(_localMoney >= card.MatchCost);
        }
    }
}
