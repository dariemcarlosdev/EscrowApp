using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EscrowApp.Models;

namespace EscrowApp.Data;

public class EscrowDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public EscrowDbContext(DbContextOptions<EscrowDbContext> options) : base(options) { }

    public DbSet<EscrowTransaction> Transactions { get; set; }

    // §0.1 Hybrid Identity pillar
    public DbSet<Actor> Actors { get; set; }
    public DbSet<IdentityMapping> IdentityMappings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EscrowDbContext).Assembly);

        // One ExternalId per Provider — prevents duplicate identity mappings
        modelBuilder.Entity<IdentityMapping>()
            .HasIndex(m => new { m.Provider, m.ExternalId })
            .IsUnique();

        // Configure ApplicationUser ↔ Actor relationship
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Actor)
            .WithMany()
            .HasForeignKey(u => u.ActorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
