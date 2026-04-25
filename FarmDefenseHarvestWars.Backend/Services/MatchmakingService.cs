using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.Extensions.Logging;

namespace FarmDefenseHarvestWars.Backend.Services;

public class MatchmakingService : IMatchmakingService
{
    private readonly object _queueLock = new();
    private readonly Queue<string> _matchQueue = [];
    private readonly HashSet<string> _queuedUsers = [];
    private readonly Dictionary<string, MatchmakingStatusDto> _activeMatches = [];
    private readonly HashSet<string> _completedMatchIds = [];

    private readonly IServiceProvider _serviceProvider;
    private readonly IMatchServerOrchestrator _matchServerOrchestrator;
    private readonly ILogger<MatchmakingService> _logger;

    public MatchmakingService(
        IServiceProvider serviceProvider,
        IMatchServerOrchestrator matchServerOrchestrator,
        ILogger<MatchmakingService> logger)
    {
        _serviceProvider = serviceProvider;
        _matchServerOrchestrator = matchServerOrchestrator;
        _logger = logger;
    }

    public async Task<MatchmakingStatusDto> QueueForMatchAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (TryGetActiveMatch(userId, out var activeMatch))
        {
            return activeMatch;
        }

        string? defenderId = null;
        string? attackerId = null;

        lock (_queueLock)
        {
            if (_activeMatches.TryGetValue(userId, out var alreadyMatched))
            {
                return alreadyMatched;
            }

            if (!_queuedUsers.Contains(userId))
            {
                _matchQueue.Enqueue(userId);
                _queuedUsers.Add(userId);
            }

            if (_matchQueue.Count >= 2)
            {
                defenderId = _matchQueue.Dequeue();
                attackerId = _matchQueue.Dequeue();
                _queuedUsers.Remove(defenderId);
                _queuedUsers.Remove(attackerId);
            }
        }

        if (defenderId != null && attackerId != null)
        {
            try
            {
                await CreateAndStoreMatchAsync(defenderId, attackerId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create match for players {DefenderId} and {AttackerId}", defenderId, attackerId);
                throw;
            }
        }

        return GetStatusForUser(userId);
    }

    public void CancelMatchmaking(string userId)
    {
        lock (_queueLock)
        {
            RemoveFromQueueInternal(userId);
        }
    }

    public MatchmakingStatusDto GetStatusForUser(string userId)
    {
        lock (_queueLock)
        {
            if (_activeMatches.TryGetValue(userId, out var active))
            {
                return active;
            }

            if (_queuedUsers.Contains(userId))
            {
                return new MatchmakingStatusDto
                {
                    IsQueued = true,
                    MatchFound = false
                };
            }

            return new MatchmakingStatusDto
            {
                IsQueued = false,
                MatchFound = false
            };
        }
    }

    public void CompleteMatch(string matchId)
    {
        lock (_queueLock)
        {
            _completedMatchIds.Add(matchId);
            RemoveActiveMatchEntriesByMatchIdInternal(matchId);
        }
    }

    private bool TryGetActiveMatch(string userId, out MatchmakingStatusDto status)
    {
        lock (_queueLock)
        {
            return _activeMatches.TryGetValue(userId, out status!);
        }
    }

    private async Task CreateAndStoreMatchAsync(
        string defenderUserId,
        string attackerUserId,
        CancellationToken cancellationToken)
    {
        // Using a scope to get IDeckService since it's likely Scoped
        using var scope = _serviceProvider.CreateScope();
        var deckService = scope.ServiceProvider.GetRequiredService<IDeckService>();

        var defenderUnits = await deckService.GetUnitCompositionAsync(defenderUserId, PlayerRole.Defender, cancellationToken);
        var attackerUnits = await deckService.GetUnitCompositionAsync(attackerUserId, PlayerRole.Attacker, cancellationToken);

        string matchId = Guid.NewGuid().ToString("N");
        var endpoint = await _matchServerOrchestrator.StartMatchServerAsync(
            matchId,
            defenderUnits,
            attackerUnits,
            cancellationToken);

        var defenderStatus = new MatchmakingStatusDto
        {
            IsQueued = false,
            MatchFound = true,
            MatchId = matchId,
            Role = PlayerRole.Defender,
            ServerAddress = endpoint.Host,
            ServerPort = endpoint.Port
        };

        var attackerStatus = new MatchmakingStatusDto
        {
            IsQueued = false,
            MatchFound = true,
            MatchId = matchId,
            Role = PlayerRole.Attacker,
            ServerAddress = endpoint.Host,
            ServerPort = endpoint.Port
        };

        lock (_queueLock)
        {
            _activeMatches[defenderUserId] = defenderStatus;
            _activeMatches[attackerUserId] = attackerStatus;
        }
    }

    private void RemoveFromQueueInternal(string userId)
    {
        if (!_queuedUsers.Remove(userId))
        {
            return;
        }

        var remaining = new Queue<string>();
        while (_matchQueue.Count > 0)
        {
            string queuedUser = _matchQueue.Dequeue();
            if (!string.Equals(queuedUser, userId, StringComparison.Ordinal))
            {
                remaining.Enqueue(queuedUser);
            }
        }

        while (remaining.Count > 0)
        {
            _matchQueue.Enqueue(remaining.Dequeue());
        }
    }

    private void RemoveActiveMatchEntriesByMatchIdInternal(string matchId)
    {
        string[] affectedUsers = _activeMatches
            .Where(kvp => string.Equals(kvp.Value.MatchId, matchId, StringComparison.Ordinal))
            .Select(kvp => kvp.Key)
            .ToArray();

        foreach (string userId in affectedUsers)
        {
            _activeMatches.Remove(userId);
            RemoveFromQueueInternal(userId);
        }
    }
}
