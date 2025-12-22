using FarmDefenseHarvestWars.Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FarmDefenseHarvestWars.Backend.Data;

// Moștenim IdentityDbContext ca să primim automat tabelele de login
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Aici vom adăuga mai târziu alte tabele (ex: Meciuri, Leaderboard)
    // public DbSet<Match> Matches { get; set; }
}
