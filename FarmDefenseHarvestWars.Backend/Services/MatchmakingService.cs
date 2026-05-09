using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Text.Json;

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
    private readonly Dictionary<string, (string DefenderId, string AttackerId)> _matchParticipants = [];
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

    public async Task CompleteMatchAsync(string matchId, MatchCompletionRequestDto request)
    {
        string? defenderId = null;
        string? attackerId = null;

        lock (_queueLock)
        {
            if (_matchParticipants.TryGetValue(matchId, out var participants))
            {
                defenderId = participants.DefenderId;
                attackerId = participants.AttackerId;
            }
            else
            {
                _logger.LogWarning("CompleteMatchAsync: Match {MatchId} not found in participants map.", matchId);
            }

            _completedMatchIds.Add(matchId);
            RemoveActiveMatchEntriesByMatchIdInternal(matchId);
            _matchParticipants.Remove(matchId);
        }

        if (defenderId != null && attackerId != null)
        {
            _logger.LogInformation("CompleteMatchAsync: Processing rewards for match {MatchId}. Def={DefenderId}, Atk={AttackerId}", matchId, defenderId, attackerId);
            await ProcessMatchRewardsAsync(matchId, defenderId, attackerId, request);
        }
        else
        {
            _logger.LogWarning("CompleteMatchAsync: Could not process rewards for match {MatchId} because participants were not resolved.", matchId);
        }
    }

    public async Task<MatchRewardDto?> GetMatchRewardAsync(string matchId, string userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await db.MatchResults.FindAsync(matchId);
        if (result == null) return null;

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return null;

        bool isDefender = result.DefenderUserId == userId;
        bool isAttacker = result.AttackerUserId == userId;

        if (!isDefender && !isAttacker) return null;

        ChestDto? droppedChest = null;
        string? droppedChestJson = isDefender ? result.DefenderDroppedChestJson : result.AttackerDroppedChestJson;
        if (!string.IsNullOrEmpty(droppedChestJson))
        {
            droppedChest = JsonSerializer.Deserialize<ChestDto>(droppedChestJson);
        }

        return new MatchRewardDto
        {
            MatchId = matchId,
            Role = isDefender ? PlayerRole.Defender : PlayerRole.Attacker,
            WinnerRole = result.WinnerRole,
            IsAborted = result.IsAborted,
            GoldEarned = isDefender ? result.DefenderGoldEarned : result.AttackerGoldEarned,
            XpEarned = isDefender ? result.DefenderXpEarned : result.AttackerXpEarned,
            TotalGoldNow = user.Gold,
            TotalXpNow = user.Xp,
            TotalLevelNow = user.Level,
            DroppedChest = droppedChest
        };
    }

    private async Task ProcessMatchRewardsAsync(string matchId, string defenderId, string attackerId, MatchCompletionRequestDto request)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var defender = await userManager.FindByIdAsync(defenderId);
            var attacker = await userManager.FindByIdAsync(attackerId);

            if (defender == null || attacker == null)
            {
                _logger.LogWarning("ProcessMatchRewardsAsync: One or both participants not found in DB. Def={DefenderId}, Atk={AttackerId}", defenderId, attackerId);
                return;
            }

            // Simple reward logic:
            // Win: 50 Gold, 100 XP
            // Loss: 20 Gold, 40 XP
            // Draw/Abort: 10 Gold, 20 XP

            int defGold = 10;
            int defXp = 20;
            int atkGold = 10;
            int atkXp = 20;

            if (!request.IsAborted && request.WinnerRole.HasValue)
            {
                if (request.WinnerRole == PlayerRole.Defender)
                {
                    defGold = 50; defXp = 100;
                    atkGold = 20; atkXp = 40;
                }
                else
                {
                    atkGold = 50; atkXp = 100;
                    defGold = 20; defXp = 40;
                }
            }

            defender.Gold += defGold;
            defender.Xp += defXp;
            // Level up logic: 1000 XP per level
            if (defender.Xp >= defender.Level * 1000)
            {
                defender.Xp -= defender.Level * 1000;
                defender.Level++;
            }

            attacker.Gold += atkGold;
            attacker.Xp += atkXp;
            if (attacker.Xp >= attacker.Level * 1000)
            {
                attacker.Xp -= attacker.Level * 1000;
                attacker.Level++;
            }

            // Chest dropping logic
            string? defChestJson = TryDropChest(defender);
            string? atkChestJson = TryDropChest(attacker);

            var result = new MatchResult
            {
                MatchId = matchId,
                DefenderUserId = defenderId,
                AttackerUserId = attackerId,
                WinnerRole = request.WinnerRole,
                IsAborted = request.IsAborted,
                DefenderGoldEarned = defGold,
                DefenderXpEarned = defXp,
                AttackerGoldEarned = atkGold,
                AttackerXpEarned = atkXp,
                DefenderDroppedChestJson = defChestJson,
                AttackerDroppedChestJson = atkChestJson,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };

            db.MatchResults.Add(result);
            await db.SaveChangesAsync();
            await userManager.UpdateAsync(defender);
            await userManager.UpdateAsync(attacker);

            _logger.LogInformation("ProcessMatchRewardsAsync: Successfully saved match results for {MatchId}.", matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessMatchRewardsAsync: Failed to process rewards for match {MatchId}.", matchId);
            throw; // Rethrow to ensure the controller knows about the failure
        }
    }

    private string? TryDropChest(ApplicationUser user)
    {
        var chestsJson = string.IsNullOrWhiteSpace(user.ChestsJson) ? "[]" : user.ChestsJson;
        var chests = JsonSerializer.Deserialize<List<ChestDto>>(chestsJson) ?? new();
        if (chests.Count >= 3)
        {
            return null;
        }

        var random = new Random();
        int roll = random.Next(100);

        string name = "Wooden Chest";
        int duration = 10; // 10 seconds for wooden chest

        if (roll > 90)
        {
            name = "Golden Chest";
            duration = 120; // 2 minutes
        }
        else if (roll > 70)
        {
            name = "Silver Chest";
            duration = 60; // 1 minute
        }

        var newChest = new ChestDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            AcquiredAt = DateTime.UtcNow,
            UnlockDurationSeconds = duration
        };

        chests.Add(newChest);
        user.ChestsJson = JsonSerializer.Serialize(chests);
        return JsonSerializer.Serialize(newChest);
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var defender = await userManager.FindByIdAsync(defenderUserId);
        var attacker = await userManager.FindByIdAsync(attackerUserId);

        var defenderUnits = await deckService.GetUnitCompositionAsync(defenderUserId, PlayerRole.Defender, cancellationToken);
        var attackerUnits = await deckService.GetUnitCompositionAsync(attackerUserId, PlayerRole.Attacker, cancellationToken);

        string matchId = Guid.NewGuid().ToString("N");
        var endpoint = await _matchServerOrchestrator.StartMatchServerAsync(
            matchId,
            defenderUnits,
            attackerUnits,
            defender?.AvatarIndex ?? 1,
            attacker?.AvatarIndex ?? 1,
            defender?.UserName ?? "Defender",
            attacker?.UserName ?? "Attacker",
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
            _matchParticipants[matchId] = (defenderUserId, attackerUserId);
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
