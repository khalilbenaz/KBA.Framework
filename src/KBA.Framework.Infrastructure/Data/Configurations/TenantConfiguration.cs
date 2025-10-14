using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KBA.Framework.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration EF Core pour l'entité Tenant
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable(KBAConsts.TablePrefix + "Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.Property(t => t.NormalizedName)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.HasIndex(t => t.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_NormalizedName");

        // Query filter pour soft delete
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
