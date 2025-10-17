using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBA.ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product Configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "Products");
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.SKU).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Category).HasMaxLength(100);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            
            entity.HasIndex(p => p.SKU).IsUnique();
            entity.HasIndex(p => p.TenantId);
            entity.HasIndex(p => p.Category);
            
            entity.HasQueryFilter(p => !p.IsDeleted);
        });
    }
}
