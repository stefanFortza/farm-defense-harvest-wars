using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class MatchRewardDto
{
    public string MatchId { get; set; } = string.Empty;
    public PlayerRole Role { get; set; }
    public PlayerRole? WinnerRole { get; set; }
    public bool IsWin => Role == WinnerRole;
    public bool IsAborted { get; set; }
    
    public int GoldEarned { get; set; }
    public int XpEarned { get; set; }
    
    public int TotalGoldNow { get; set; }
    public int TotalXpNow { get; set; }
    public int TotalLevelNow { get; set; }
}
