using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class DeckDto
{
    public int Id { get; set; }
    public PlayerRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<UnitType> Units { get; set; } = [];
}
