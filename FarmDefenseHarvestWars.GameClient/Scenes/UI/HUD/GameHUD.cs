using Godot;
using Godot.Collections;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay.GameplayManagers;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.GameClient.Core.Utils;
using FarmDefenseHarvestWars.GameClient.Scripts.Utils;
using FarmDefenseHarvestWars.GameClient.Scenes.Gameplay;
using FarmDefenseHarvestWars.GameClient.Scenes.UI;

public partial class GameHUD : CanvasLayer, IInitializable<GameHudContext>
{
    [Export] private ResourcePanel _resourcePanel = null!;
    [Export] private HBoxContainer _deckContainer = null!;
    [Export] private ProgressBar _timerBar = null!;
    [Export] private Label _timerLabel = null!; // Optional: Numeric display
    [Export] private ProgressBar _baseHealthBar = null!;
    [Export] private PackedScene _cardScene = null!;
    [Export] private TextureRect _defenderAvatar = null!;
    [Export] private TextureRect _attackerAvatar = null!;
    [Export] private Label _defenderNameLabel = null!;
    [Export] private Label _attackerNameLabel = null!;
    [Export] private Button _settingsButton = null!;
    [Export] private Button _quitButton = null!;
    [Export] private PackedScene _settingsPopupScene = null!;

    private MatchManager _matchManager = null!;
    private InputController _inputController = null!;
    private UnitRegistry _unitRegistry = null!;

    private readonly Array<UnitCard> _cards = [];
    private readonly Dictionary<UnitType, UnitCard> _cardsByType = [];
    private long _localPeerId;
    private int _localMoney;
    private float _matchDuration;
    private PlayerRole? _assignedRole;
    private bool _isReady;
    private bool _isBound;
    private bool _isGameStateBound;

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
        _assignedRole = data.AssignedRole;


        IsInitialized = true;

        if (_isReady)
        {
            ApplyInitialization();
        }
    }

    public override void _Ready()
    {
        this.EnsureNotNull(_resourcePanel, nameof(_resourcePanel));
        this.EnsureNotNull(_deckContainer, nameof(_deckContainer));
        this.EnsureNotNull(_timerBar, nameof(_timerBar));
        this.EnsureNotNull(_timerLabel, nameof(_timerLabel));
        this.EnsureNotNull(_settingsButton, nameof(_settingsButton));
        this.EnsureNotNull(_quitButton, nameof(_quitButton));

        _settingsButton.Pressed += OnSettingsPressed;
        _quitButton.Pressed += OnQuitPressed;

        _localPeerId = Multiplayer.GetUniqueId();
        _isReady = true;

        if (IsInitialized)
        {
            ApplyInitialization();
        }
    }

    private void OnSettingsPressed()
    {
        AudioController.Instance?.PlaySfx("res://Assets/Audio/ui/click1.ogg");

        if (_settingsPopupScene != null)
        {
            var popup = _settingsPopupScene.Instantiate<Control>();
            AddChild(popup);
            UIAnimations.AnimatePop(popup);
        }
    }

    private void OnQuitPressed()
    {
        AudioController.Instance?.PlaySfx("res://Assets/Audio/ui/click1.ogg");
        NetworkBootstrap.Instance?.Gameplay?.Disconnect();
        GetTree().ChangeSceneToFile("res://Scenes/Menus/MainMenu/MainMenu.tscn");
    }

    public override void _ExitTree()
    {
        if (_isBound)
        {
            _matchManager.MoneyChanged -= OnMoneyChanged;
            _matchManager.TimerUpdated -= OnTimerUpdated;
            _matchManager.BaseHealthChanged -= OnBaseHealthChanged;
            _inputController.PlacementResolved -= OnPlacementResolved;
        }

        if (_isGameStateBound && GameState.Instance != null)
        {
            GameState.Instance.RoleAssigned -= OnRoleAssigned;
            GameState.Instance.DeckUpdated -= OnDeckUpdated;
            GameState.Instance.MatchConfigurationLoaded -= OnMatchConfigurationLoaded;
            _isGameStateBound = false;
        }
    }

    private void ApplyInitialization()
    {
        if (_isBound)
        {
            return;
        }

        _unitRegistry.InitializeLookup();
        UpdatePlayerInfo();
        BindGameplaySignals();
        RebuildDeck();
        RefreshAffordability();
        _resourcePanel?.UpdateDisplay(_localMoney);

        _isBound = true;
    }

    private void UpdatePlayerInfo()
    {
        if (GameState.Instance == null) return;

        int defIndex = GameState.Instance.DefenderAvatarIndex;
        int atkIndex = GameState.Instance.AttackerAvatarIndex;

        if (_defenderAvatar != null && _unitRegistry.Avatars.Count > defIndex)
        {
            _defenderAvatar.Texture = _unitRegistry.Avatars[defIndex];
        }

        if (_attackerAvatar != null && _unitRegistry.Avatars.Count > atkIndex)
        {
            _attackerAvatar.Texture = _unitRegistry.Avatars[atkIndex];
        }

        if (_defenderNameLabel != null)
        {
            _defenderNameLabel.Text = GameState.Instance.DefenderName;
        }

        if (_attackerNameLabel != null)
        {
            _attackerNameLabel.Text = GameState.Instance.AttackerName;
        }
    }

    private void BindGameplaySignals()
    {
        _matchManager.MoneyChanged += OnMoneyChanged;
        _matchManager.TimerUpdated += OnTimerUpdated;
        _matchManager.BaseHealthChanged += OnBaseHealthChanged;

        // Initialize display
        OnBaseHealthChanged(_matchManager.GetBaseHealth(), _matchManager.MaxBaseHealth);

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

        if (!_isGameStateBound && GameState.Instance != null)
        {
            GameState.Instance.RoleAssigned += OnRoleAssigned;
            GameState.Instance.DeckUpdated += OnDeckUpdated;
            GameState.Instance.MatchConfigurationLoaded += OnMatchConfigurationLoaded;
            _isGameStateBound = true;
        }

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

        if (GodotObject.IsInstanceValid(_timerLabel))
        {
            int totalSeconds = Mathf.CeilToInt(timeRemaining);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timerLabel.Text = $"{minutes:00}:{seconds:00}";

            // Visual feedback: color the timer red when time is running out (< 30s)
            if (timeRemaining < 30f)
            {
                _timerLabel.AddThemeColorOverride("font_color", Colors.Crimson);
            }
            else
            {
                _timerLabel.RemoveThemeColorOverride("font_color");
            }
        }
    }

    private void OnBaseHealthChanged(int currentHealth, int maxHealth)
    {
        if (_baseHealthBar != null)
        {
            _baseHealthBar.MaxValue = maxHealth;
            _baseHealthBar.Value = currentHealth;
        }
    }

    private void PopulateDeck(Array<UnitData> units)
    {
        foreach (Node child in _deckContainer.GetChildren())
        {
            child.QueueFree();
        }
        _cards.Clear();
        _cardsByType.Clear();

        foreach (var unitData in units)
        {
            if (unitData == null)
            {
                continue;
            }

            var card = _cardScene.Instantiate<UnitCard>();
            card.Setup(unitData);
            card.CardPressed += OnCardPressed;

            _deckContainer.AddChild(card);
            _cards.Add(card);
            _cardsByType[unitData.Type] = card;
        }

        RefreshAffordability();
    }

    private void RebuildDeck()
    {
        if (!_isReady || !IsInitialized)
        {
            return;
        }

        if (!TryResolveAssignedRole(out PlayerRole role))
        {
            PopulateDeck([]);
            return;
        }

        PopulateDeck(BuildOwnDeckData(role));
    }

    private bool TryResolveAssignedRole(out PlayerRole role)
    {
        if (_assignedRole.HasValue)
        {
            role = _assignedRole.Value;
            return true;
        }

        var state = GameState.Instance;
        if (state?.HasAssignedRole == true)
        {
            PlayerRole? assignedRole = state.AssignedRole;
            if (assignedRole.HasValue)
            {
                _assignedRole = assignedRole;
                role = assignedRole.Value;
                return true;
            }
        }

        role = default;
        return false;
    }

    private Array<UnitData> BuildOwnDeckData(PlayerRole role)
    {
        var result = new Array<UnitData>();
        var selectedUnits = GameState.Instance?.GetMyMatchDeck();


        if (selectedUnits != null)
        {
            foreach (var unlock in selectedUnits)
            {
                UnitData unitData = _unitRegistry.GetUnitData(unlock.UnitType);
                if (unitData != null && unitData.Role == role)
                {
                    result.Add(unitData);
                }
            }
        }

        return result;
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

    private void OnRoleAssigned(int role)
    {
        _assignedRole = (PlayerRole)role;
        RebuildDeck();
    }

    private void OnDeckUpdated(int role)
    {
        if (_assignedRole.HasValue && role != (int)_assignedRole.Value)
        {
            return;
        }

        RebuildDeck();
    }

    private void OnMatchConfigurationLoaded()
    {
        UpdatePlayerInfo();
        RebuildDeck();
    }
}
