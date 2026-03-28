using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class MatchmakingStatusDto
{
    public bool IsQueued { get; set; }
    public bool MatchFound { get; set; }
    public string? MatchId { get; set; }
    public PlayerRole? Role { get; set; }
    public string? ServerAddress { get; set; }
    public int? ServerPort { get; set; }
}
