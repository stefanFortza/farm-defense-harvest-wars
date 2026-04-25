using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.Extensions.Logging;

namespace FarmDefenseHarvestWars.Backend.Services;

public class MatchmakingService : IMatchmakingService
{
    private readonly object _queueLock = new();
    
    // Separate queues for each role preference
    private readonly Queue<string> _defenderQueue = [];
    private readonly Queue<string> _attackerQueue = [];
    private readonly Queue<string> _anyQueue = [];
    
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

    public async Task<MatchmakingStatusDto> QueueForMatchAsync(string userId, PlayerRole preferredRole = PlayerRole.Any, CancellationToken cancellationToken = default)
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
                // Try to match immediately
                if (preferredRole == PlayerRole.Defender)
                {
                    if (_attackerQueue.Count > 0)
                    {
                        defenderId = userId;
                        attackerId = _attackerQueue.Dequeue();
                        _queuedUsers.Remove(attackerId);
                    }
                    else if (_anyQueue.Count > 0)
                    {
                        defenderId = userId;
                        attackerId = _anyQueue.Dequeue();
                        _queuedUsers.Remove(attackerId);
                    }
                    else
                    {
                        _defenderQueue.Enqueue(userId);
                        _queuedUsers.Add(userId);
                    }
                }
                else if (preferredRole == PlayerRole.Attacker)
                {
                    if (_defenderQueue.Count > 0)
                    {
                        attackerId = userId;
                        defenderId = _defenderQueue.Dequeue();
                        _queuedUsers.Remove(defenderId);
                    }
                    else if (_anyQueue.Count > 0)
                    {
                        attackerId = userId;
                        defenderId = _anyQueue.Dequeue();
                        _queuedUsers.Remove(defenderId);
                    }
                    else
                    {
                        _attackerQueue.Enqueue(userId);
                        _queuedUsers.Add(userId);
                    }
                }
                else // PlayerRole.Any
                {
                    if (_defenderQueue.Count > 0)
                    {
                        attackerId = userId;
                        defenderId = _defenderQueue.Dequeue();
                        _queuedUsers.Remove(defenderId);
                    }
                    else if (_attackerQueue.Count > 0)
                    {
                        defenderId = userId;
                        attackerId = _attackerQueue.Dequeue();
                        _queuedUsers.Remove(attackerId);
                    }
                    else if (_anyQueue.Count > 0)
                    {
                        defenderId = _anyQueue.Dequeue();
                        attackerId = userId;
                        _queuedUsers.Remove(defenderId);
                    }
                    else
                    {
                        _anyQueue.Enqueue(userId);
                        _queuedUsers.Add(userId);
                    }
                }
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
                // If match creation fails, we could potentially put them back in queue, 
                // but for now we throw and let them try again.
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

        RemoveFromSpecificQueue(_defenderQueue, userId);
        RemoveFromSpecificQueue(_attackerQueue, userId);
        RemoveFromSpecificQueue(_anyQueue, userId);
    }

    private static void RemoveFromSpecificQueue(Queue<string> queue, string userId)
    {
        if (!queue.Contains(userId)) return;

        var remaining = new Queue<string>();
        while (queue.Count > 0)
        {
            string queuedUser = queue.Dequeue();
            if (!string.Equals(queuedUser, userId, StringComparison.Ordinal))
            {
                remaining.Enqueue(queuedUser);
            }
        }

        while (remaining.Count > 0)
        {
            queue.Enqueue(remaining.Dequeue());
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
