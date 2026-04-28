using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Godot;
using Refit;

public partial class MenuNetwork : Node
{
    private CancellationTokenSource? _matchmakingCts;

    public bool IsMatchmakingActive => _matchmakingCts is { IsCancellationRequested: false };

    public async Task<DeckDto> UpdateDeckForRoleAsync(PlayerRole role, IReadOnlyList<UnitType> units)
    {
        var request = new UpdateDeckDto
        {
            Name = $"{role} Deck",
            Units = [.. units]
        };

        return await NetworkBootstrap.Instance.ApiClient.UpdateDeckAsync(role, request);
    }

    public async Task<bool> SyncDeckForRoleFromServerAsync(PlayerRole role, bool skipIfSaveInFlight = true)
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
        var localDeck = state.GetSelectedDeckForRoleSnapshot(role);
        if (AreDecksEqual(localDeck, serverDeck.Units))
        {
            return false;
        }

        state.SetDeckForRole(role, serverDeck.Units);
        return true;
    }

    public async Task<(bool DefenderChanged, bool AttackerChanged)> SyncAllDecksFromServerAsync(bool skipIfSaveInFlight = true)
    {
        var state = GameState.Instance;
        if (state == null)
        {
            return (false, false);
        }

        var defenderTask = NetworkBootstrap.Instance.ApiClient.GetDeckAsync(PlayerRole.Defender);
        var attackerTask = NetworkBootstrap.Instance.ApiClient.GetDeckAsync(PlayerRole.Attacker);
        await Task.WhenAll(defenderTask, attackerTask);

        bool defenderChanged = false;
        bool attackerChanged = false;

        if (!(skipIfSaveInFlight && state.IsDeckSaveInProgress(PlayerRole.Defender)))
        {
            var localDefender = state.GetSelectedDeckForRoleSnapshot(PlayerRole.Defender);
            var serverDefender = defenderTask.Result.Units;
            if (!AreDecksEqual(localDefender, serverDefender))
            {
                state.SetDeckForRole(PlayerRole.Defender, serverDefender);
                defenderChanged = true;
            }
        }

        if (!(skipIfSaveInFlight && state.IsDeckSaveInProgress(PlayerRole.Attacker)))
        {
            var localAttacker = state.GetSelectedDeckForRoleSnapshot(PlayerRole.Attacker);
            var serverAttacker = attackerTask.Result.Units;
            if (!AreDecksEqual(localAttacker, serverAttacker))
            {
                state.SetDeckForRole(PlayerRole.Attacker, serverAttacker);
                attackerChanged = true;
            }
        }

        return (defenderChanged, attackerChanged);
    }

    public async Task<PlayerProfileDto> UnlockUnitAsync(UnitType unitType)
    {
        var profile = await NetworkBootstrap.Instance.ApiClient.UnlockUnitAsync(unitType);
        GameState.Instance.SetProfile(profile);
        return profile;
    }

    public async Task<PlayerProfileDto> GetProfileAsync()
    {
        var profile = await NetworkBootstrap.Instance.ApiClient.GetProfileAsync();
        GameState.Instance.SetProfile(profile);
        return profile;
    }

    public async Task<PlayerProfileDto> UpdateAvatarAsync(int avatarIndex)
    {
        var profile = await NetworkBootstrap.Instance.ApiClient.UpdateAvatarAsync(avatarIndex);
        GameState.Instance.SetProfile(profile);
        return profile;
    }

    public async Task<ChestOpenResultDto> OpenChestAsync(string chestId)
    {
        var result = await NetworkBootstrap.Instance.ApiClient.OpenChestAsync(chestId);
        GameState.Instance.SetProfile(result.Profile);
        return result;
    }

    public async Task<PlayerProfileDto> StartUnlockChestAsync(string chestId)
    {
        var profile = await NetworkBootstrap.Instance.ApiClient.StartUnlockChestAsync(chestId);
        GameState.Instance.SetProfile(profile);
        return profile;
    }

    public async Task<PlayerProfileDto> UpgradeUnitAsync(UnitType unitType)
    {
        var profile = await NetworkBootstrap.Instance.ApiClient.UpgradeUnitAsync(unitType);
        GameState.Instance.SetProfile(profile);
        return profile;
    }

    public async Task QueueForMatchAsync(PlayerRole preferredRole = PlayerRole.Any)
    {
        if (IsMatchmakingActive)
        {
            throw new InvalidOperationException("Matchmaking is already active.");
        }

        _matchmakingCts = new CancellationTokenSource();
        try
        {
            await NetworkBootstrap.Instance.ApiClient.QueueForMatchAsync(preferredRole);
        }
        catch
        {
            _matchmakingCts.Dispose();
            _matchmakingCts = null;
            throw;
        }
    }

    public async Task<MatchmakingStatusDto?> StartMatchmakingUntilFoundAsync(PlayerRole preferredRole = PlayerRole.Any, double pollIntervalSeconds = 1.0)
    {
        await QueueForMatchAsync(preferredRole);
        return await PollMatchStatusUntilFoundAsync(pollIntervalSeconds);
    }

    public async Task<MatchmakingStatusDto?> PollMatchStatusUntilFoundAsync(double pollIntervalSeconds = 1.0)
    {
        if (_matchmakingCts == null)
        {
            throw new InvalidOperationException("Matchmaking was not started.");
        }

        var interval = TimeSpan.FromSeconds(Math.Max(0.2, pollIntervalSeconds));
        var token = _matchmakingCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var status = await NetworkBootstrap.Instance.ApiClient.GetMatchmakingStatusAsync();
                if (status.MatchFound)
                {
                    return status;
                }

                await Task.Delay(interval, token);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            _matchmakingCts?.Dispose();
            _matchmakingCts = null;
        }
    }

    public async Task CancelMatchmakingAsync()
    {
        _matchmakingCts?.Cancel();

        try
        {
            await NetworkBootstrap.Instance.ApiClient.CancelMatchmakingAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            GD.Print("Matchmaking was already canceled server-side.");
        }
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
}
