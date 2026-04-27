using FarmDefenseHarvestWars.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace FarmDefenseHarvestWars.Backend.Models;

public class MatchResult
{
    [Key]
    public string MatchId { get; set; } = string.Empty;
    
    public string DefenderUserId { get; set; } = string.Empty;
    public string AttackerUserId { get; set; } = string.Empty;
    
    public PlayerRole? WinnerRole { get; set; }
    public bool IsAborted { get; set; }
    
    public int DefenderGoldEarned { get; set; }
    public int DefenderXpEarned { get; set; }
    
    public int AttackerGoldEarned { get; set; }
    public int AttackerXpEarned { get; set; }
    
    public string? DefenderDroppedChestJson { get; set; }
    public string? AttackerDroppedChestJson { get; set; }
    
    public DateTimeOffset CompletedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
