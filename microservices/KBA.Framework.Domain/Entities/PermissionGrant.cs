using KBA.Framework.Domain.Common;

namespace KBA.Framework.Domain.Entities;

/// <summary>
/// Attribution de permission (Grant)
/// </summary>
public class PermissionGrant : Entity
{
    public Guid? TenantId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty; // "User" ou "Role"
    public string ProviderKey { get; set; } = string.Empty; // UserId ou RoleId

    // Navigation properties
    public virtual Permission? Permission { get; set; }

    public PermissionGrant()
    {
    }

    public PermissionGrant(string permissionName, string providerName, string providerKey, Guid? tenantId = null)
    {
        PermissionName = permissionName;
        ProviderName = providerName;
        ProviderKey = providerKey;
        TenantId = tenantId;
    }

    public void Delete()
    {
        MarkAsDeleted();
    }
}
