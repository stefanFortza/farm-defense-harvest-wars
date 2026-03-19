using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class UpdateDeckDto
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<UnitType> Units { get; set; } = [];
}
