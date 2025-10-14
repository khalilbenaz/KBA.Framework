using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KBA.Framework.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration EF Core pour l'entité Role
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(KBAConsts.TablePrefix + "Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.Property(r => r.NormalizedName)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.Property(r => r.Description)
            .HasMaxLength(KBAConsts.MaxDescriptionLength);

        builder.HasIndex(r => r.NormalizedName)
            .HasDatabaseName("IX_Roles_NormalizedName");

        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_Roles_TenantId");

        // Query filter pour soft delete
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
