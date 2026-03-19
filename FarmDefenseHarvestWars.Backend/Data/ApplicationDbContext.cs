using FarmDefenseHarvestWars.Backend.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

    public DbSet<Deck> Decks => Set<Deck>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureDeck(builder.Entity<Deck>());
    }

    private static void ConfigureDeck(EntityTypeBuilder<Deck> deck)
    {
        deck.HasKey(x => x.Id);

        deck.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        deck.Property(x => x.UnitCompositionJson)
            .IsRequired();

        deck.HasIndex(x => new { x.UserId, x.Role })
            .IsUnique();

        deck.HasOne(x => x.User)
            .WithMany(x => x.Decks)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
