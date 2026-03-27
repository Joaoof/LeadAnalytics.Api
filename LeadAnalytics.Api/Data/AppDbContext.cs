using LeadAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeadAnalytics.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("leads");

            entity.HasKey(e => e.Id);

            // Não pode ter dois leads com o mesmo ExternalId + TenantId
            entity.HasIndex(e => new { e.ExternalId, e.TenantId })
                  .IsUnique();

            // Índice pra busca rápida por clínica e data
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
        });
    }
}
