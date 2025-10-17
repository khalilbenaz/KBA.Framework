using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBA.PermissionService.Data;

public class PermissionDbContext : DbContext
{
    public PermissionDbContext(DbContextOptions<PermissionDbContext> options) : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Permission Configuration
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "Permissions");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name).IsRequired().HasMaxLength(128);
            entity.Property(p => p.DisplayName).IsRequired().HasMaxLength(256);
            entity.Property(p => p.GroupName).HasMaxLength(128);

            entity.HasIndex(p => p.Name).IsUnique();
            entity.HasIndex(p => p.GroupName);

            // Relation hiérarchique
            entity.HasOne(p => p.Parent)
                  .WithMany(p => p.Children)
                  .HasForeignKey(p => p.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        // PermissionGrant Configuration
        modelBuilder.Entity<PermissionGrant>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "PermissionGrants");
            entity.HasKey(pg => pg.Id);

            entity.Property(pg => pg.PermissionName).IsRequired().HasMaxLength(128);
            entity.Property(pg => pg.ProviderName).IsRequired().HasMaxLength(64);
            entity.Property(pg => pg.ProviderKey).IsRequired().HasMaxLength(64);

            entity.HasIndex(pg => new { pg.PermissionName, pg.ProviderName, pg.ProviderKey })
                  .IsUnique();

            entity.HasIndex(pg => pg.TenantId);
            entity.HasQueryFilter(pg => !pg.IsDeleted);
        });

        // Seed des permissions de base
        SeedPermissions(modelBuilder);
    }

    private void SeedPermissions(ModelBuilder modelBuilder)
    {
        var permissions = new List<Permission>
        {
            // Users
            new Permission("Users.View", "Voir les utilisateurs", "Users"),
            new Permission("Users.Create", "Créer des utilisateurs", "Users"),
            new Permission("Users.Edit", "Modifier des utilisateurs", "Users"),
            new Permission("Users.Delete", "Supprimer des utilisateurs", "Users"),

            // Products
            new Permission("Products.View", "Voir les produits", "Products"),
            new Permission("Products.Create", "Créer des produits", "Products"),
            new Permission("Products.Edit", "Modifier des produits", "Products"),
            new Permission("Products.Delete", "Supprimer des produits", "Products"),
            new Permission("Products.ManageStock", "Gérer le stock", "Products"),

            // Tenants
            new Permission("Tenants.View", "Voir les tenants", "Tenants"),
            new Permission("Tenants.Create", "Créer des tenants", "Tenants"),
            new Permission("Tenants.Edit", "Modifier des tenants", "Tenants"),
            new Permission("Tenants.Delete", "Supprimer des tenants", "Tenants"),

            // Permissions
            new Permission("Permissions.View", "Voir les permissions", "Permissions"),
            new Permission("Permissions.Grant", "Accorder des permissions", "Permissions"),
            new Permission("Permissions.Revoke", "Révoquer des permissions", "Permissions"),

            // System
            new Permission("System.Settings", "Paramètres système", "System"),
            new Permission("System.Audit", "Voir les logs d'audit", "System"),
        };

        // On ne peut pas utiliser SetCreationInfo ici car on n'a pas de contexte utilisateur
        // On va définir les IDs manuellement pour le seed
        for (int i = 0; i < permissions.Count; i++)
        {
            // Utiliser reflection pour définir l'ID
            var idProperty = typeof(Permission).GetProperty("Id");
            idProperty?.SetValue(permissions[i], Guid.NewGuid());
        }

        modelBuilder.Entity<Permission>().HasData(permissions);
    }
}
