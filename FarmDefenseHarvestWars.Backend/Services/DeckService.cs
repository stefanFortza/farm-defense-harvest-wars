using FarmDefenseHarvestWars.Backend.Data;
using FarmDefenseHarvestWars.Backend.Models;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmDefenseHarvestWars.Backend.Services;

public class DeckService : IDeckService
{
    private const int MaxDeckCards = 6;
    private static readonly JsonSerializerOptions DeckSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApplicationDbContext _db;
    private readonly IUnitRegistryProvider _unitRegistryProvider;
    private readonly IProfileService _profileService;

    public DeckService(
        ApplicationDbContext db,
        IUnitRegistryProvider unitRegistryProvider,
        IProfileService profileService)
    {
        _db = db;
        _unitRegistryProvider = unitRegistryProvider;
        _profileService = profileService;
    }

    public async Task<DeckDto> GetDeckAsync(string userId, PlayerRole role, CancellationToken cancellationToken = default)
    {
        Deck deck = await GetOrCreateDeckInternalAsync(userId, role, cancellationToken);
        return ToDeckDto(deck);
    }

    public DeckDto GetDefaultDeck(PlayerRole role)
    {
        return new DeckDto
        {
            Id = 0,
            Role = role,
            Name = $"{role} Starter",
            Units = _unitRegistryProvider.GetDefaultUnitsForRole(role, MaxDeckCards)
        };
    }

    public async Task<DeckDto> UpdateDeckAsync(string userId, PlayerRole role, UpdateDeckDto request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateDeckRequest(role, request, _unitRegistryProvider);
        if (validationError != null)
        {
            throw new ArgumentException(validationError);
        }

        HashSet<UnitType> unlockedUnits = await _profileService.GetUnlockedUnitTypesForRoleAsync(userId, role, cancellationToken);
        if (request.Units.Any(unit => !unlockedUnits.Contains(unit)))
        {
            throw new InvalidOperationException($"Deck contains units that are not unlocked for role {role}.");
        }

        Deck deck = await GetOrCreateDeckInternalAsync(userId, role, cancellationToken);
        deck.Name = string.IsNullOrWhiteSpace(request.Name) ? $"{role} Deck" : request.Name.Trim();
        deck.UnitCompositionJson = SerializeUnits(request.Units);

        await _db.SaveChangesAsync(cancellationToken);

        return ToDeckDto(deck);
    }

    public async Task<IReadOnlyList<UnitUnlockDto>> GetUnitCompositionAsync(string userId, PlayerRole role, CancellationToken cancellationToken = default)
    {
        Deck deck = await GetOrCreateDeckInternalAsync(userId, role, cancellationToken);
        var unitTypes = DeserializeUnits(deck.UnitCompositionJson);
        
        var unlocks = await _db.UnitUnlocks
            .Where(u => u.UserId == userId && unitTypes.Contains(u.UnitType))
            .ToListAsync(cancellationToken);
            
        return unitTypes.Select(type => {
            var unlock = unlocks.FirstOrDefault(u => u.UnitType == type);
            return new UnitUnlockDto {
                UnitType = type,
                Level = unlock?.Level ?? 1,
                Fragments = unlock?.Fragments ?? 0
            };
        }).ToList();
    }

    private async Task<Deck> GetOrCreateDeckInternalAsync(string userId, PlayerRole role, CancellationToken cancellationToken)
    {
        Deck? existingDeck = await _db.Decks
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Role == role, cancellationToken);

        if (existingDeck != null)
        {
            return existingDeck;
        }

        HashSet<UnitType> unlockedUnits = await _profileService.GetUnlockedUnitTypesForRoleAsync(userId, role, cancellationToken);

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
}
