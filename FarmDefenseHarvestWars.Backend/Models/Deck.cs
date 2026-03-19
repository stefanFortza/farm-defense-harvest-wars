using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Backend.Models;

public class Deck
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public PlayerRole Role { get; set; }
    public string Name { get; set; } = "Starter Deck";
    public string UnitCompositionJson { get; set; } = "[]";
}
