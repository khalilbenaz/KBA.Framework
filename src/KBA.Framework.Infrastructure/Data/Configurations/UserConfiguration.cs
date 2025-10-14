using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KBA.Framework.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration EF Core pour l'entité User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(KBAConsts.TablePrefix + "Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.Property(u => u.NormalizedUserName)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxEmailLength);

        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(KBAConsts.MaxEmailLength);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(KBAConsts.MaxPhoneLength);

        builder.Property(u => u.FirstName)
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.Property(u => u.LastName)
            .HasMaxLength(KBAConsts.MaxNameLength);

        builder.HasIndex(u => u.NormalizedUserName)
            .HasDatabaseName("IX_Users_NormalizedUserName");

        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("IX_Users_NormalizedEmail");

        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_Users_TenantId");

        // Query filter pour soft delete
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
