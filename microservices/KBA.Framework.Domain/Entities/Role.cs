using KBA.Framework.Domain.Common;

namespace KBA.Framework.Domain.Entities;

/// <summary>
/// Entité rôle pour les microservices
/// </summary>
public class Role : Entity
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public bool IsStatic { get; set; } = false;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public Role()
    {
    }

    public Role(string name, string displayName, string? description = null, bool isStatic = false)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        IsStatic = isStatic;
    }
}
