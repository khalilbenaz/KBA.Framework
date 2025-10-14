using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities.Auditing;
using KBA.Framework.Domain.Entities.BackgroundJobs;
using KBA.Framework.Domain.Entities.Configuration;
using KBA.Framework.Domain.Entities.Identity;
using KBA.Framework.Domain.Entities.MultiTenancy;
using KBA.Framework.Domain.Entities.Organization;
using KBA.Framework.Domain.Entities.Permissions;
using KBA.Framework.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;

namespace KBA.Framework.Infrastructure.Data;

/// <summary>
/// Contexte de base de données principal du framework KBA
/// </summary>
public class KBADbContext : DbContext
{
    /// <summary>
    /// Constructeur
    /// </summary>
    public KBADbContext(DbContextOptions<KBADbContext> options) : base(options)
    {
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<RoleClaim> RoleClaims => Set<RoleClaim>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();

    // Multi-Tenancy
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantConnectionString> TenantConnectionStrings => Set<TenantConnectionString>();

    // Permissions
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    // Audit Logging
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditLogAction> AuditLogActions => Set<AuditLogAction>();
    public DbSet<EntityChange> EntityChanges => Set<EntityChange>();
    public DbSet<EntityPropertyChange> EntityPropertyChanges => Set<EntityPropertyChange>();

    // Configuration
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<FeatureValue> FeatureValues => Set<FeatureValue>();

    // Organization
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<UserOrganizationUnit> UserOrganizationUnits => Set<UserOrganizationUnit>();

    // Background Jobs
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();

    // Business Entities
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Configuration du modèle
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Application des configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KBADbContext).Assembly);
    }
}
