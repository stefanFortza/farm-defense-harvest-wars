using Microsoft.AspNetCore.Identity;

namespace FarmDefenseHarvestWars.Backend.Models;

public class ApplicationUser : IdentityUser
{
    // Aici adăugăm datele specifice jocului
    public int Gold { get; set; } = 100; // Începe cu 100 aur
    public int Level { get; set; } = 1;
    public int Xp { get; set; } = 0;

    // Putem salva deck-ul ca un string JSON simplu pentru început
    // Ex: "['cow_unit', 'chicken_unit']"
    public string UnlockedUnits { get; set; } = "[]";
}
