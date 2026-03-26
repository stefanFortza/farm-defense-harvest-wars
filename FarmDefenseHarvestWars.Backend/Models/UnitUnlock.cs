using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Backend.Models;

public class UnitUnlock
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public PlayerRole Role { get; set; }
    public UnitType UnitType { get; set; }
    public DateTime UnlockedAt { get; set; }
}
