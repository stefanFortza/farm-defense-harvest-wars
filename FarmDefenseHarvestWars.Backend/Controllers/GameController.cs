using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Backend.Services;
using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmDefenseHarvestWars.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // <--- ASTA E CHEIA: Nimeni nu intră aici fără token valid!
public class GameController : ControllerBase
{
    private const int MaxDeckCards = 5;
    private static readonly object QueueLock = new();
    private static readonly Queue<string> MatchQueue = [];
    private static readonly HashSet<string> QueuedUsers = [];
    private static readonly Dictionary<string, MatchmakingStatusDto> ActiveMatches = [];

    private static readonly JsonSerializerOptions DeckSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IDefaultUnitUnlockService _defaultUnitUnlockService;
    private readonly IMatchServerOrchestrator _matchServerOrchestrator;
    private readonly IUnitRegistryProvider _unitRegistryProvider;
    private readonly ILogger<GameController> _logger;

    public GameController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IDefaultUnitUnlockService defaultUnitUnlockService,
        IMatchServerOrchestrator matchServerOrchestrator,
        IUnitRegistryProvider unitRegistryProvider,
        ILogger<GameController> logger)
    {
        _userManager = userManager;
        _db = db;
        _defaultUnitUnlockService = defaultUnitUnlockService;
        _matchServerOrchestrator = matchServerOrchestrator;
        _unitRegistryProvider = unitRegistryProvider;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        await _defaultUnitUnlockService.EnsureDefaultUnlocksAsync(user.Id, cancellationToken);
        var profile = await BuildPlayerProfileAsync(user, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("deck/{role}")]
    public async Task<ActionResult<DeckDto>> GetDeck(PlayerRole role, CancellationToken cancellationToken)
    {
        if (!IsDeckRole(role))
        {
            return BadRequest("Role must be Defender or Attacker.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        Deck deck = await GetOrCreateDeckAsync(user.Id, role, cancellationToken);
        return Ok(ToDeckDto(deck));
    }

    [HttpGet("deck/{role}/default")]
    public ActionResult<DeckDto> GetDefaultDeck(PlayerRole role)
    {
        if (!IsDeckRole(role))
        {
            return BadRequest("Role must be Defender or Attacker.");
        }

        return Ok(new DeckDto
        {
            Id = 0,
            Role = role,
            Name = $"{role} Starter",
            Units = _unitRegistryProvider.GetDefaultUnitsForRole(role, MaxDeckCards)
        });
    }

    [HttpPut("deck/{role}")]
    public async Task<ActionResult<DeckDto>> UpdateDeck(
        PlayerRole role,
        [FromBody] UpdateDeckDto request,
        CancellationToken cancellationToken)
    {
        if (!IsDeckRole(role))
        {
            return BadRequest("Role must be Defender or Attacker.");
        }

        var validationError = ValidateDeckRequest(role, request, _unitRegistryProvider);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        HashSet<UnitType> unlockedUnits = await GetUnlockedUnitTypesForRoleAsync(user.Id, role, cancellationToken);
        if (request.Units.Any(unit => !unlockedUnits.Contains(unit)))
        {
            return BadRequest($"Deck contains units that are not unlocked for role {role}.");
        }

        Deck deck = await GetOrCreateDeckAsync(user.Id, role, cancellationToken);
        deck.Name = string.IsNullOrWhiteSpace(request.Name) ? $"{role} Deck" : request.Name.Trim();
        deck.UnitCompositionJson = SerializeUnits(request.Units);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToDeckDto(deck));
    }

    [HttpPost("unit/{unitType}/unlock")]
    public async Task<ActionResult<PlayerProfileDto>> UnlockUnit(UnitType unitType, CancellationToken cancellationToken)
    {
        if (unitType == UnitType.None)
        {
            return BadRequest("Unit type is invalid.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        UnitDataDto? unitData = _unitRegistryProvider.GetUnit(unitType);
        if (unitData == null)
        {
            return BadRequest("Unknown unit.");
        }

        if (unitData.IsDefaultUnlocked)
        {
            return BadRequest("Unit is unlocked by default.");
        }

        PlayerRole? role = ResolveRoleForUnit(unitType, unitData.Role);
        if (!role.HasValue)
        {
            return BadRequest("Could not resolve unit role.");
        }

        bool alreadyUnlocked = await _db.UnitUnlocks.AnyAsync(
            unlock => unlock.UserId == user.Id && unlock.Role == role.Value && unlock.UnitType == unitType,
            cancellationToken);
        if (alreadyUnlocked)
        {
            return BadRequest("Unit already unlocked.");
        }

        if (user.Gold < unitData.UnlockCost)
        {
            return BadRequest($"Not enough gold. Required: {unitData.UnlockCost}, available: {user.Gold}.");
        }

        user.Gold -= unitData.UnlockCost;
        _db.UnitUnlocks.Add(new UnitUnlock
        {
            UserId = user.Id,
            Role = role.Value,
            UnitType = unitType,
            UnlockedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        PlayerProfileDto profile = await BuildPlayerProfileAsync(user, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("matchmaking/queue")]
    public async Task<ActionResult<MatchmakingStatusDto>> QueueForMatch(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        if (TryGetActiveMatch(user.Id, out var activeMatch))
        {
            return Ok(activeMatch);
        }

        string? defenderId = null;
        string? attackerId = null;

        lock (QueueLock)
        {
            if (ActiveMatches.TryGetValue(user.Id, out var alreadyMatched))
            {
                return Ok(alreadyMatched);
            }

            if (!QueuedUsers.Contains(user.Id))
            {
                MatchQueue.Enqueue(user.Id);
                QueuedUsers.Add(user.Id);
            }

            if (MatchQueue.Count >= 2)
            {
                defenderId = MatchQueue.Dequeue();
                attackerId = MatchQueue.Dequeue();
                QueuedUsers.Remove(defenderId);
                QueuedUsers.Remove(attackerId);
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
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to start match server.");
            }
        }

        return Ok(GetStatusForUser(user.Id));
    }

    [HttpDelete("matchmaking/queue")]
    public ActionResult CancelMatchmaking()
    {
        string? userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User not found.");
        }

        lock (QueueLock)
        {
            RemoveFromQueue(userId);
        }

        return NoContent();
    }

    [HttpGet("matchmaking/status")]
    public ActionResult<MatchmakingStatusDto> GetMatchmakingStatus()
    {
        string? userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User not found.");
        }

        return Ok(GetStatusForUser(userId));
    }

    private static bool IsDeckRole(PlayerRole role)
    {
        return role is PlayerRole.Defender or PlayerRole.Attacker;
    }

    private static string? ValidateDeckRequest(PlayerRole role, UpdateDeckDto request, IUnitRegistryProvider unitRegistryProvider)
    {
        if (request.Units == null || request.Units.Count == 0)
        {
            return "Deck must contain at least one unit.";
        }

        if (request.Units.Count > MaxDeckCards)
        {
            return $"Deck can contain at most {MaxDeckCards} units.";
        }

        if (request.Units.Distinct().Count() != request.Units.Count)
        {
            return "Deck cannot contain duplicate units.";
        }

        if (request.Units.Any(unit => !unitRegistryProvider.UnitExists(unit)))
        {
            return "Deck contains unknown units that are missing from UnitRegistry.json.";
        }

        if (request.Units.Any(unit => !unitRegistryProvider.IsRoleCompatible(unit, role)))
        {
            return $"Deck contains units that are not valid for role {role}.";
        }

        return null;
    }

    private async Task<Deck> GetOrCreateDeckAsync(string userId, PlayerRole role, CancellationToken cancellationToken)
    {
        Deck? existingDeck = await _db.Decks
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Role == role, cancellationToken);

        if (existingDeck != null)
        {
            return existingDeck;
        }

        HashSet<UnitType> unlockedUnits = await GetUnlockedUnitTypesForRoleAsync(userId, role, cancellationToken);

        var deck = new Deck
        {
            UserId = userId,
            Role = role,
            Name = $"{role} Starter",
            UnitCompositionJson = SerializeUnits(unlockedUnits.Take(MaxDeckCards).ToArray())
        };

        _db.Decks.Add(deck);
        await _db.SaveChangesAsync(cancellationToken);
        return deck;
    }

    private static DeckDto ToDeckDto(Deck deck)
    {
        return new DeckDto
        {
            Id = deck.Id,
            Role = deck.Role,
            Name = deck.Name,
            Units = DeserializeUnits(deck.UnitCompositionJson)
        };
    }

    private static IReadOnlyList<UnitType> DeserializeUnits(string unitCompositionJson)
    {
        if (string.IsNullOrWhiteSpace(unitCompositionJson))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<UnitType>>(unitCompositionJson, DeckSerializerOptions) ?? [];
    }

    private static string SerializeUnits(IReadOnlyCollection<UnitType> units)
    {
        return JsonSerializer.Serialize(units, DeckSerializerOptions);
    }

    private async Task CreateAndStoreMatchAsync(
        string defenderUserId,
        string attackerUserId,
        CancellationToken cancellationToken)
    {
        Deck defenderDeck = await GetOrCreateDeckAsync(defenderUserId, PlayerRole.Defender, cancellationToken);
        Deck attackerDeck = await GetOrCreateDeckAsync(attackerUserId, PlayerRole.Attacker, cancellationToken);

        string matchId = Guid.NewGuid().ToString("N");
        var endpoint = await _matchServerOrchestrator.StartMatchServerAsync(
            matchId,
            DeserializeUnits(defenderDeck.UnitCompositionJson),
            DeserializeUnits(attackerDeck.UnitCompositionJson),
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

        lock (QueueLock)
        {
            ActiveMatches[defenderUserId] = defenderStatus;
            ActiveMatches[attackerUserId] = attackerStatus;
        }
    }

    private static bool TryGetActiveMatch(string userId, out MatchmakingStatusDto status)
    {
        lock (QueueLock)
        {
            return ActiveMatches.TryGetValue(userId, out status!);
        }
    }

    private static MatchmakingStatusDto GetStatusForUser(string userId)
    {
        lock (QueueLock)
        {
            if (ActiveMatches.TryGetValue(userId, out var active))
            {
                return active;
            }

            if (QueuedUsers.Contains(userId))
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

    private static void RemoveFromQueue(string userId)
    {
        if (!QueuedUsers.Remove(userId))
        {
            return;
        }

        var remaining = new Queue<string>();
        while (MatchQueue.Count > 0)
        {
            string queuedUser = MatchQueue.Dequeue();
            if (!string.Equals(queuedUser, userId, StringComparison.Ordinal))
            {
                remaining.Enqueue(queuedUser);
            }
        }

        while (remaining.Count > 0)
        {
            MatchQueue.Enqueue(remaining.Dequeue());
        }
    }

    private async Task<PlayerProfileDto> BuildPlayerProfileAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        PlayerUnlockedUnitsDto unlockedUnits = await GetUnlockedUnitsDtoAsync(user.Id, cancellationToken);

        return new PlayerProfileDto
        {
            Email = user.Email!,
            Gold = user.Gold,
            Level = user.Level,
            Xp = user.Xp,
            UnlockedUnits = unlockedUnits
        };
    }

    private async Task<PlayerUnlockedUnitsDto> GetUnlockedUnitsDtoAsync(string userId, CancellationToken cancellationToken)
    {
        List<UnitUnlock> unlocks = await _db.UnitUnlocks
            .AsNoTracking()
            .Where(unlock => unlock.UserId == userId)
            .ToListAsync(cancellationToken);

        return new PlayerUnlockedUnitsDto
        {
            DefenderUnits = unlocks
                .Where(unlock => unlock.Role == PlayerRole.Defender)
                .Select(unlock => unlock.UnitType)
                .Distinct()
                .OrderBy(unit => unit)
                .ToArray(),
            AttackerUnits = unlocks
                .Where(unlock => unlock.Role == PlayerRole.Attacker)
                .Select(unlock => unlock.UnitType)
                .Distinct()
                .OrderBy(unit => unit)
                .ToArray()
        };
    }

    private async Task<HashSet<UnitType>> GetUnlockedUnitTypesForRoleAsync(
        string userId,
        PlayerRole role,
        CancellationToken cancellationToken)
    {
        List<UnitType> unlockedUnits = await _db.UnitUnlocks
            .AsNoTracking()
            .Where(unlock => unlock.UserId == userId && unlock.Role == role)
            .Select(unlock => unlock.UnitType)
            .ToListAsync(cancellationToken);

        return unlockedUnits.ToHashSet();
    }

    private PlayerRole? ResolveRoleForUnit(UnitType unitType, PlayerRole? unitRole)
    {
        if (unitRole.HasValue)
        {
            return unitRole.Value;
        }

        bool compatibleWithDefender = _unitRegistryProvider.IsRoleCompatible(unitType, PlayerRole.Defender);
        bool compatibleWithAttacker = _unitRegistryProvider.IsRoleCompatible(unitType, PlayerRole.Attacker);

        if (compatibleWithDefender && !compatibleWithAttacker)
        {
            return PlayerRole.Defender;
        }

        if (compatibleWithAttacker && !compatibleWithDefender)
        {
            return PlayerRole.Attacker;
        }

        return null;
    }
}