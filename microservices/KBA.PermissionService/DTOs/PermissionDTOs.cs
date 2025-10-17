using KBA.Framework.Domain.Common;

namespace KBA.PermissionService.DTOs;

public record PermissionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? GroupName { get; init; }
    public Guid? ParentId { get; init; }
    public List<PermissionDto> Children { get; init; } = new();
}

public record PermissionGrantDto
{
    public Guid Id { get; init; }
    public Guid? TenantId { get; init; }
    public string PermissionName { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ProviderKey { get; init; } = string.Empty;
    public DateTime GrantedAt { get; init; }
}

public record CreatePermissionDto
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? GroupName { get; init; }
    public Guid? ParentId { get; init; }
}

public record GrantPermissionDto
{
    public string PermissionName { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty; // "User" ou "Role"
    public string ProviderKey { get; init; } = string.Empty;  // UserId ou RoleId
    public Guid? TenantId { get; init; }
}

public record RevokePermissionDto
{
    public string PermissionName { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ProviderKey { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
}

public record CheckPermissionDto
{
    public Guid UserId { get; init; }
    public string PermissionName { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
}

public record PermissionCheckResult
{
    public bool IsGranted { get; init; }
    public string PermissionName { get; init; } = string.Empty;
    public string? GrantedBy { get; init; } // "User" ou "Role"
}

/// <summary>
/// Paramètres de recherche des permissions
/// </summary>
public class PermissionSearchParams : PaginationParams
{
    public string? SearchTerm { get; set; }
    public string? GroupName { get; set; }
    public string? ProviderName { get; set; } // Filtrer les grants par provider
    public string? ProviderKey { get; set; }
}
