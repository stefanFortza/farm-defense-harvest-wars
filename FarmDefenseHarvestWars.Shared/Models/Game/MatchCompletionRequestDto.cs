using FarmDefenseHarvestWars.Shared.Enums;

namespace FarmDefenseHarvestWars.Shared.Models.Game;

public class MatchCompletionRequestDto
{
    public PlayerRole? WinnerRole { get; set; }
    public string TerminationReason { get; set; } = string.Empty;
    public bool IsAborted { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
