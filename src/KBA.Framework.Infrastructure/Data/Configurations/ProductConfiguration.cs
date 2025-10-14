using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KBA.Framework.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration EF Core pour l'entité Product
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(KBAConsts.TablePrefix + "Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.Property(p => p.Description)
            .HasMaxLength(KBAConsts.MaxDescriptionLength);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Stock)
            .IsRequired();

        builder.Property(p => p.SKU)
            .HasMaxLength(100);

        builder.Property(p => p.Category)
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_Products_Name");

        builder.HasIndex(p => p.SKU)
            .HasDatabaseName("IX_Products_SKU");

        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_Products_TenantId");

        // Ignorer les événements de domaine (non persistés)
        builder.Ignore(p => p.DomainEvents);

        // Query filter pour soft delete
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
