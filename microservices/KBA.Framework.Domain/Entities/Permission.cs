using KBA.Framework.Domain.Common;

namespace KBA.Framework.Domain.Entities;

/// <summary>
/// Entité permission pour le système de permissions
/// </summary>
public class Permission : Entity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public Guid? ParentId { get; set; }

    // Navigation properties
    public virtual Permission? Parent { get; set; }
    public virtual ICollection<Permission> Children { get; set; } = new List<Permission>();
    public virtual ICollection<PermissionGrant> Grants { get; set; } = new List<PermissionGrant>();

    public Permission()
    {
    }

    public Permission(string name, string displayName, string? description = null)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
    }

    public Permission(string name, string displayName, string? description, string? groupName)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        GroupName = groupName;
    }

    public void Delete()
    {
        MarkAsDeleted();
    }
}
