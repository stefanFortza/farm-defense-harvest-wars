using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class UnitDataDto
{
    public UnitType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MatchCost { get; set; }
    public int UnlockCost { get; set; }
    public bool IsDefaultUnlocked { get; set; }
    public int MaxHealth { get; set; }
    public int Damage { get; set; }
    public float AttackRange { get; set; }
    public float AttackSpeed { get; set; }
}
