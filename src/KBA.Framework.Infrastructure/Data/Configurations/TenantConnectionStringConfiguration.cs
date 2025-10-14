using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KBA.Framework.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration EF Core pour l'entité TenantConnectionString
/// </summary>
public class TenantConnectionStringConfiguration : IEntityTypeConfiguration<TenantConnectionString>
{
    public void Configure(EntityTypeBuilder<TenantConnectionString> builder)
    {
        builder.ToTable(KBAConsts.TablePrefix + "TenantConnectionStrings");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.Property(t => t.Value)
            .IsRequired()
            .HasMaxLength(1024);

        builder.HasOne(t => t.Tenant)
            .WithMany(t => t.ConnectionStrings)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TenantId)
            .HasDatabaseName("IX_TenantConnectionStrings_TenantId");
    }
}
