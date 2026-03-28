using LeadAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeadAnalytics.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Lead> Leads { get; set; }
    public DbSet<Unit> Units { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("leads");

            entity.HasKey(e => e.Id);

            // Não pode ter dois leads com o mesmo ExternalId + TenantId
            entity.HasIndex(e => new { e.Id, e.TenantId })
                  .IsUnique();

            // Índice pra busca rápida por clínica e data
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });

            // Relacionamento Lead → Unit
            modelBuilder.Entity<Lead>()
                .HasOne(l => l.Unit)
                .WithMany(u => u.Leads)
                .HasForeignKey(l => l.UnitId);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("units");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.Id, e.ClinicId }).IsUnique();

            entity.HasIndex(e => new { e.ClinicId, e.CreatedAt });
        });
    }
}
