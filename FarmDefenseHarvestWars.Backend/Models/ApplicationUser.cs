using Microsoft.AspNetCore.Identity;

namespace FarmDefenseHarvestWars.Backend.Models;

public class ApplicationUser : IdentityUser
{
    // Aici adăugăm datele specifice jocului
    public int Gold { get; set; } = 100; // Începe cu 100 aur
    public int Level { get; set; } = 1;
    public int Xp { get; set; } = 0;
    public int AvatarIndex { get; set; } = 1; // Default avatar index (1-8)

    public string ChestsJson { get; set; } = "[]"; // Max 5 chests

    public ICollection<Deck> Decks { get; set; } = [];
    public ICollection<UnitUnlock> UnitUnlocks { get; set; } = [];
}
