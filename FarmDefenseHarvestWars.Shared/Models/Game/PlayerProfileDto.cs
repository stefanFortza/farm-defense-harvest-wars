namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class PlayerProfileDto
{
    public string Email { get; set; } = string.Empty;
    public int Gold { get; set; }
    public int Level { get; set; }
    public int Xp { get; set; }
    // Vom adăuga unitățile mai târziu
}