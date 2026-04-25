using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.GameClient.Scripts.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using Godot;
using Refit;

public partial class DeckService : Node
{
    public const int MaxCards = 6;

    public static DeckService Instance { get; private set; } = null!;

    private readonly object _saveSync = new();
    private readonly Dictionary<PlayerRole, List<UnitType>> _pendingDeckByRole = [];
    private readonly Dictionary<PlayerRole, long> _latestVersionByRole = [];
    private readonly HashSet<PlayerRole> _isProcessingRole = [];

    public override void _Ready()
    {
        Instance = this;
    }

    public bool CanEditDeckInMenu(PlayerRole role)
    {
        var state = GameState.Instance;
        if (state == null)
        {
            return false;
        }

        if (state.HasAssignedRole)
        {
            return false;
        }

        if (role != PlayerRole.Attacker && role != PlayerRole.Defender)
        {
            return false;
        }

        return IsMenuSceneActive();
    }

    public List<UnitType> GetDeckForRole(PlayerRole role, UnitRegistry registry)
    {
        var result = new List<UnitType>(MaxCards);
        var state = GameState.Instance;
        if (state == null)
        {
            return result;
        }

        var source = state.GetSelectedDeckForRoleSnapshot(role);

        foreach (var unitType in source)
        {
            if (!IsRoleCompatible(registry, unitType, role))
            {
                continue;
            }

            if (result.Contains(unitType))
            {
                continue;
            }

            result.Add(unitType);
            if (result.Count >= MaxCards)
            {
                break;
            }
        }

        return result;
    }

    public void SubmitDeckSaveForRole(PlayerRole role, IReadOnlyList<UnitType> units, UnitRegistry registry)
    {
        if (!CanEditDeckInMenu(role))
        {
            return;
        }

        var normalized = NormalizeDeckForRole(role, units, registry);
        bool shouldStartLoop;

        lock (_saveSync)
        {
            _pendingDeckByRole[role] = normalized;

            long currentVersion = _latestVersionByRole.TryGetValue(role, out var version)
                ? version
                : 0;
            _latestVersionByRole[role] = currentVersion + 1;

            shouldStartLoop = _isProcessingRole.Add(role);
        }

        if (shouldStartLoop)
        {
            _ = ProcessSaveLoop(role);
        }
    }

    public List<UnitData> GetCompatibleUnits(UnitRegistry registry, PlayerRole role)
    {
        var result = new List<UnitData>();
        foreach (var unit in registry.AllUnits)
        {
            if (unit == null)
            {
                continue;
            }

            if (unit.Role != role)
            {
                continue;
            }

            result.Add(unit);
        }

        return result;
    }

    public bool IsRoleCompatible(UnitRegistry registry, UnitType unitType, PlayerRole role)
    {
        return registry.IsRoleCompatible(unitType, role);
    }

    public async Task<bool> SyncDeckForRoleFromServerAsync(PlayerRole role, UnitRegistry registry, bool skipIfSaveInFlight = true)
    {
        var state = GameState.Instance;
        if (state == null)
        {
            return false;
        }

        if (skipIfSaveInFlight && state.IsDeckSaveInProgress(role))
        {
            return false;
        }

        var serverDeck = await NetworkBootstrap.Instance.ApiClient.GetDeckAsync(role);
        var localDeck = GetDeckForRole(role, registry);
        var normalizedServer = NormalizeDeckForRole(role, serverDeck.Units, registry);
        if (AreDecksEqual(localDeck, normalizedServer))
        {
            return false;
        }

        state.SetDeckForRole(role, normalizedServer);
        return true;
    }

    private async Task ProcessSaveLoop(PlayerRole role)
    {
        while (true)
        {
            List<UnitType>? requestedDeck;
            long requestVersion;

            lock (_saveSync)
            {
                if (!_pendingDeckByRole.TryGetValue(role, out requestedDeck))
                {
                    _isProcessingRole.Remove(role);
                    return;
                }

                _pendingDeckByRole.Remove(role);
                requestVersion = _latestVersionByRole[role];
            }

            GameState.Instance?.SetDeckSaveInProgress(role, true);

            try
            {
                var updatedDeck = await NetworkBootstrap.Instance.Menu.UpdateDeckForRoleAsync(role, requestedDeck);

                if (!IsLatestVersion(role, requestVersion))
                {
                    continue;
                }

                GameState.Instance?.SetDeckForRole(role, updatedDeck.Units);
                GameState.Instance?.NotifyDeckSaveResult(role, true, string.Empty);
            }
            catch (ApiException ex)
            {
                GD.PrintErr($"Failed to save deck for role {role}: {ex.Message}");

                if (IsLatestVersion(role, requestVersion))
                {
                    GameState.Instance?.NotifyDeckSaveResult(role, false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Unexpected error while saving deck for role {role}: {ex.Message}");

                if (IsLatestVersion(role, requestVersion))
                {
                    GameState.Instance?.NotifyDeckSaveResult(role, false, "Unexpected deck save error.");
                }
            }
        }
    }

    private bool IsLatestVersion(PlayerRole role, long version)
    {
        lock (_saveSync)
        {
            return _latestVersionByRole.TryGetValue(role, out var latestVersion)
                && latestVersion == version;
        }
    }

    private static List<UnitType> NormalizeDeckForRole(PlayerRole role, IReadOnlyList<UnitType> units, UnitRegistry registry)
    {
        var normalized = new List<UnitType>(MaxCards);

        foreach (var unit in units)
        {
            if (normalized.Count >= MaxCards)
            {
                break;
            }

            if (!registry.IsRoleCompatible(unit, role))
            {
                continue;
            }

            if (normalized.Contains(unit))
            {
                continue;
            }

            normalized.Add(unit);
        }

        return normalized;
    }

    private static bool AreDecksEqual(IReadOnlyList<UnitType> left, IReadOnlyList<UnitType> right)
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

    private static bool IsMenuSceneActive()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return false;
        }

        string scenePath = tree.CurrentScene?.SceneFilePath ?? string.Empty;
        return scenePath.StartsWith("res://Scenes/Menus/", StringComparison.Ordinal);
    }
}
