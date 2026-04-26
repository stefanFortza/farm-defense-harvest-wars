namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class PlayerProfileDto
{
    public string Email { get; set; } = string.Empty;
    public int Gold { get; set; }
    public int Level { get; set; }
    public int Xp { get; set; }
    public int AvatarIndex { get; set; }
    public PlayerUnlockedUnitsDto UnlockedUnits { get; set; } = new();
}