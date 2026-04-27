using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class UnitUnlockDto
{
    public UnitType UnitType { get; set; }
    public int Level { get; set; } = 1;
    public int Fragments { get; set; } = 0;
    public int FragmentsRequiredForNextLevel => Level * 10; // Simple growth formula
    public int UpgradeCost => Level * 100; // Simple cost formula
}
