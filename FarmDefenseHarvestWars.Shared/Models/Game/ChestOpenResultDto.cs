namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class ChestOpenResultDto
{
    public PlayerProfileDto Profile { get; set; } = null!;
    public List<UnitUnlockDto> Rewards { get; set; } = [];
}
