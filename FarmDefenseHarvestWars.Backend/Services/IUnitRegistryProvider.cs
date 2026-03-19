using FarmDefenseHarvestWars.Shared.Enums;
using FarmDefenseHarvestWars.Shared.Models.Game;

namespace FarmDefenseHarvestWars.Backend.Services;

public interface IUnitRegistryProvider
{
    IReadOnlyList<UnitDataDto> GetAllUnits();
    IReadOnlyList<UnitType> GetDefaultUnitsForRole(PlayerRole role, int maxCards);
    bool IsRoleCompatible(UnitType unitType, PlayerRole role);
    bool UnitExists(UnitType unitType);
}
