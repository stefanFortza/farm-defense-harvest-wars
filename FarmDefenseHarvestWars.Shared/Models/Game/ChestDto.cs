namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class ChestDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Wooden Chest";
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}
