using System.Text.Json;
using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public sealed class UnitRegistryProvider : IUnitRegistryProvider
{
    private static readonly HashSet<UnitType> DefenderUnits =
    [
        UnitType.Cow,
        UnitType.Chicken,
        UnitType.Sheep,
        UnitType.Pig
    ];

    private static readonly HashSet<UnitType> AttackerUnits =
    [
        UnitType.Wolf,
        UnitType.Fox,
        UnitType.Bear,
        UnitType.Skeleton
    ];

    private readonly Lazy<UnitRegistryDto> _registry;

    public UnitRegistryProvider(IWebHostEnvironment environment)
    {
        _registry = new Lazy<UnitRegistryDto>(() => LoadRegistry(environment.ContentRootPath));
    }

    public IReadOnlyList<UnitDataDto> GetAllUnits() => _registry.Value.Units;

    public IReadOnlyList<UnitType> GetDefaultUnitsForRole(PlayerRole role, int maxCards)
    {
        return GetDefaultUnlockedUnitsForRole(role)
            .Take(maxCards)
            .ToList();
    }

    public IReadOnlyList<UnitType> GetDefaultUnlockedUnitsForRole(PlayerRole role)
    {
        return _registry.Value.Units
            .Where(unit => unit.IsDefaultUnlocked && IsRoleCompatible(unit.Type, role))
            .Select(unit => unit.Type)
            .Distinct()
            .ToList();
    }

    public UnitDataDto? GetUnit(UnitType unitType)
    {
        return _registry.Value.Units.FirstOrDefault(unit => unit.Type == unitType);
    }

    public bool IsRoleCompatible(UnitType unitType, PlayerRole role)
    {
        UnitDataDto? unit = _registry.Value.Units.FirstOrDefault(x => x.Type == unitType);
        if (unit?.Role is PlayerRole unitRole)
        {
            return unitRole == role;
        }

        return role switch
        {
            PlayerRole.Defender => DefenderUnits.Contains(unitType),
            PlayerRole.Attacker => AttackerUnits.Contains(unitType),
            _ => false
        };
    }

    public bool UnitExists(UnitType unitType)
    {
        return _registry.Value.Units.Any(unit => unit.Type == unitType);
    }

    private static UnitRegistryDto LoadRegistry(string contentRootPath)
    {
        string registryPath = Path.Combine(contentRootPath, "Data", "Units", "UnitRegistry.json");
        if (!File.Exists(registryPath))
        {
            throw new FileNotFoundException("Unit registry file not found.", registryPath);
        }

        string json = File.ReadAllText(registryPath);
        var registry = JsonSerializer.Deserialize<UnitRegistryDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (registry == null)
        {
            throw new InvalidOperationException("Unit registry file is invalid JSON.");
        }

        return registry;
    }
}
