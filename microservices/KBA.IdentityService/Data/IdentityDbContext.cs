using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBA.IdentityService.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "Users");
            entity.HasKey(u => u.Id);
            
            entity.Property(u => u.UserName).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.TenantId);
            
            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        // Role Configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "Roles");
            entity.HasKey(r => r.Id);
            
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.Property(r => r.DisplayName).HasMaxLength(200);
            entity.Property(r => r.Description).HasMaxLength(500);
            
            entity.HasIndex(r => new { r.TenantId, r.Name }).IsUnique();
            
            entity.HasQueryFilter(r => !r.IsDeleted);
        });

        // Seed Default Admin Role
        modelBuilder.Entity<Role>().HasData(
            new Role("Admin", "Administrateur", "Accès complet au système", true)
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111")
            },
            new Role("User", "Utilisateur", "Utilisateur standard", true)
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222")
            }
        );
    }
}
