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

        var updatedDeck = await NetworkBootstrap.Instance.ApiClient.UpdateDeckAsync(role, request);
        GameState.Instance.SetDeckForRole(role, updatedDeck.Units);
        return updatedDeck;
    }

    public async Task<PlayerProfileDto> UnlockUnitAsync(UnitType unitType)
    {
        var profile = await NetworkBootstrap.Instance.ApiClient.UnlockUnitAsync(unitType);
        GameState.Instance.SetProfile(profile);
        return profile;
    }

    public async Task QueueForMatchAsync()
    {
        if (IsMatchmakingActive)
        {
            throw new InvalidOperationException("Matchmaking is already active.");
        }

        _matchmakingCts = new CancellationTokenSource();
        try
        {
            await NetworkBootstrap.Instance.ApiClient.QueueForMatchAsync();
        }
        catch
        {
            _matchmakingCts.Dispose();
            _matchmakingCts = null;
            throw;
        }
    }

    public async Task<MatchmakingStatusDto?> StartMatchmakingUntilFoundAsync(double pollIntervalSeconds = 1.0)
    {
        await QueueForMatchAsync();
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
}