using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBA.TenantService.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "Tenants");
            entity.HasKey(t => t.Id);
            
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(t => t.Name).IsUnique();
            
            entity.HasQueryFilter(t => !t.IsDeleted);
        });
    }
}
