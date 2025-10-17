using KBA.Framework.Domain.Common;
using KBA.Framework.Domain.Entities;
using KBA.PermissionService.Data;
using KBA.PermissionService.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace KBA.PermissionService.Services;

public interface IPermissionServiceLogic
{
    Task<List<PermissionDto>> GetAllAsync();
    Task<PagedResult<PermissionDto>> SearchAsync(PermissionSearchParams searchParams);
    Task<PermissionDto?> GetByIdAsync(Guid id);
    Task<PermissionDto?> GetByNameAsync(string name);
    Task<List<PermissionDto>> GetByGroupAsync(string groupName);
    Task<PermissionDto> CreateAsync(CreatePermissionDto dto);
    Task DeleteAsync(Guid id);
    
    // Permission Grants
    Task<PermissionCheckResult> CheckPermissionAsync(CheckPermissionDto dto);
    Task<bool> GrantPermissionAsync(GrantPermissionDto dto);
    Task<bool> RevokePermissionAsync(RevokePermissionDto dto);
    Task<List<PermissionGrantDto>> GetUserPermissionsAsync(Guid userId, Guid? tenantId = null);
    Task<List<PermissionGrantDto>> GetRolePermissionsAsync(Guid roleId, Guid? tenantId = null);
}

public class PermissionServiceLogic : IPermissionServiceLogic
{
    private readonly PermissionDbContext _context;
    private readonly ILogger<PermissionServiceLogic> _logger;
    private readonly IDistributedCache _cache;
    private const int CacheExpirationMinutes = 15;

    public PermissionServiceLogic(
        PermissionDbContext context, 
        ILogger<PermissionServiceLogic> logger,
        IDistributedCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<PermissionDto>> GetAllAsync()
    {
        var permissions = await _context.Permissions
            .Include(p => p.Children)
            .Where(p => p.ParentId == null) // Seulement les racines
            .OrderBy(p => p.GroupName)
            .ThenBy(p => p.DisplayName)
            .ToListAsync();

        return permissions.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<PermissionDto>> SearchAsync(PermissionSearchParams searchParams)
    {
        var query = _context.Permissions.AsQueryable();

        // Filtrage par terme de recherche
        if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
        {
            var searchTerm = searchParams.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                p.DisplayName.ToLower().Contains(searchTerm));
        }

        // Filtrage par groupe
        if (!string.IsNullOrWhiteSpace(searchParams.GroupName))
        {
            query = query.Where(p => p.GroupName == searchParams.GroupName);
        }

        var totalCount = await query.CountAsync();

        // Tri et pagination
        var permissions = await query
            .OrderBy(p => p.GroupName)
            .ThenBy(p => p.DisplayName)
            .Skip(searchParams.Skip)
            .Take(searchParams.PageSize)
            .ToListAsync();

        var permissionDtos = permissions.Select(MapToDtoSimple).ToList();

        return new PagedResult<PermissionDto>(
            permissionDtos,
            totalCount,
            searchParams.PageNumber,
            searchParams.PageSize
        );
    }

    public async Task<PermissionDto?> GetByIdAsync(Guid id)
    {
        var permission = await _context.Permissions
            .Include(p => p.Children)
            .FirstOrDefaultAsync(p => p.Id == id);

        return permission == null ? null : MapToDto(permission);
    }

    public async Task<PermissionDto?> GetByNameAsync(string name)
    {
        var permission = await _context.Permissions
            .Include(p => p.Children)
            .FirstOrDefaultAsync(p => p.Name == name);

        return permission == null ? null : MapToDto(permission);
    }

    public async Task<List<PermissionDto>> GetByGroupAsync(string groupName)
    {
        var permissions = await _context.Permissions
            .Where(p => p.GroupName == groupName)
            .OrderBy(p => p.DisplayName)
            .ToListAsync();

        return permissions.Select(MapToDtoSimple).ToList();
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionDto dto)
    {
        var existing = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Name == dto.Name);

        if (existing != null)
            throw new InvalidOperationException($"Une permission avec le nom '{dto.Name}' existe déjà");

        var permission = new Permission(dto.Name, dto.DisplayName, null, dto.GroupName)
        {
            ParentId = dto.ParentId
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Permission created: {PermissionName}", permission.Name);

        return MapToDtoSimple(permission);
    }

    public async Task DeleteAsync(Guid id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
            throw new KeyNotFoundException($"Permission {id} not found");

        permission.Delete();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Permission deleted: {PermissionId}", id);
    }

    // ============ PERMISSION GRANTS ============

    public async Task<PermissionCheckResult> CheckPermissionAsync(CheckPermissionDto dto)
    {
        // Vérifier le cache d'abord
        var cacheKey = $"permission:{dto.UserId}:{dto.PermissionName}:{dto.TenantId}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            var cached = JsonSerializer.Deserialize<PermissionCheckResult>(cachedResult);
            if (cached != null)
            {
                _logger.LogDebug("Permission check from cache: {UserId} - {Permission}", dto.UserId, dto.PermissionName);
                return cached;
            }
        }

        // Vérifier les grants directs de l'utilisateur
        var userGrant = await _context.PermissionGrants
            .FirstOrDefaultAsync(pg =>
                pg.ProviderName == "User" &&
                pg.ProviderKey == dto.UserId.ToString() &&
                pg.PermissionName == dto.PermissionName &&
                pg.TenantId == dto.TenantId);

        if (userGrant != null)
        {
            var result = new PermissionCheckResult
            {
                IsGranted = true,
                PermissionName = dto.PermissionName,
                GrantedBy = "User"
            };

            await CachePermissionCheckAsync(cacheKey, result);
            return result;
        }

        // TODO: Vérifier les grants via les rôles
        // Nécessite d'appeler Identity Service pour obtenir les rôles de l'utilisateur
        // Pour l'instant, on retourne non accordé

        var notGrantedResult = new PermissionCheckResult
        {
            IsGranted = false,
            PermissionName = dto.PermissionName
        };

        await CachePermissionCheckAsync(cacheKey, notGrantedResult);
        return notGrantedResult;
    }

    public async Task<bool> GrantPermissionAsync(GrantPermissionDto dto)
    {
        // Vérifier que la permission existe
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Name == dto.PermissionName);

        if (permission == null)
            throw new KeyNotFoundException($"Permission '{dto.PermissionName}' not found");

        // Vérifier si le grant existe déjà
        var existing = await _context.PermissionGrants
            .FirstOrDefaultAsync(pg =>
                pg.PermissionName == dto.PermissionName &&
                pg.ProviderName == dto.ProviderName &&
                pg.ProviderKey == dto.ProviderKey &&
                pg.TenantId == dto.TenantId);

        if (existing != null)
        {
            _logger.LogWarning("Permission already granted: {Permission} to {Provider}:{Key}", 
                dto.PermissionName, dto.ProviderName, dto.ProviderKey);
            return false;
        }

        var grant = new PermissionGrant(
            dto.PermissionName,
            dto.ProviderName,
            dto.ProviderKey,
            dto.TenantId
        );

        _context.PermissionGrants.Add(grant);
        await _context.SaveChangesAsync();

        // Invalider le cache
        await InvalidatePermissionCacheAsync(dto.ProviderKey, dto.PermissionName, dto.TenantId);

        _logger.LogInformation(
            "Permission granted: {Permission} to {Provider}:{Key}",
            dto.PermissionName,
            dto.ProviderName,
            dto.ProviderKey
        );

        return true;
    }

    public async Task<bool> RevokePermissionAsync(RevokePermissionDto dto)
    {
        var grant = await _context.PermissionGrants
            .FirstOrDefaultAsync(pg =>
                pg.PermissionName == dto.PermissionName &&
                pg.ProviderName == dto.ProviderName &&
                pg.ProviderKey == dto.ProviderKey &&
                pg.TenantId == dto.TenantId);

        if (grant == null)
        {
            _logger.LogWarning("Permission not found for revoke: {Permission} from {Provider}:{Key}",
                dto.PermissionName, dto.ProviderName, dto.ProviderKey);
            return false;
        }

        grant.Delete();
        await _context.SaveChangesAsync();

        // Invalider le cache
        await InvalidatePermissionCacheAsync(dto.ProviderKey, dto.PermissionName, dto.TenantId);

        _logger.LogInformation(
            "Permission revoked: {Permission} from {Provider}:{Key}",
            dto.PermissionName,
            dto.ProviderName,
            dto.ProviderKey
        );

        return true;
    }

    public async Task<List<PermissionGrantDto>> GetUserPermissionsAsync(Guid userId, Guid? tenantId = null)
    {
        var query = _context.PermissionGrants
            .Where(pg => pg.ProviderName == "User" && pg.ProviderKey == userId.ToString());

        if (tenantId.HasValue)
            query = query.Where(pg => pg.TenantId == tenantId.Value);

        var grants = await query.OrderBy(pg => pg.PermissionName).ToListAsync();
        return grants.Select(MapGrantToDto).ToList();
    }

    public async Task<List<PermissionGrantDto>> GetRolePermissionsAsync(Guid roleId, Guid? tenantId = null)
    {
        var query = _context.PermissionGrants
            .Where(pg => pg.ProviderName == "Role" && pg.ProviderKey == roleId.ToString());

        if (tenantId.HasValue)
            query = query.Where(pg => pg.TenantId == tenantId.Value);

        var grants = await query.OrderBy(pg => pg.PermissionName).ToListAsync();
        return grants.Select(MapGrantToDto).ToList();
    }

    // ============ HELPERS ============

    private async Task CachePermissionCheckAsync(string cacheKey, PermissionCheckResult result)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
        };

        var json = JsonSerializer.Serialize(result);
        await _cache.SetStringAsync(cacheKey, json, options);
    }

    private async Task InvalidatePermissionCacheAsync(string providerKey, string permissionName, Guid? tenantId)
    {
        // On ne peut pas énumérer les clés dans IDistributedCache facilement
        // Pour une vraie production, utiliser Redis directement ou un pattern de cache plus sophistiqué
        var cacheKey = $"permission:{providerKey}:{permissionName}:{tenantId}";
        await _cache.RemoveAsync(cacheKey);
    }

    private static PermissionDto MapToDto(Permission permission)
    {
        return new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            DisplayName = permission.DisplayName,
            GroupName = permission.GroupName,
            ParentId = permission.ParentId,
            Children = permission.Children.Select(MapToDtoSimple).ToList()
        };
    }

    private static PermissionDto MapToDtoSimple(Permission permission)
    {
        return new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            DisplayName = permission.DisplayName,
            GroupName = permission.GroupName,
            ParentId = permission.ParentId,
            Children = new List<PermissionDto>()
        };
    }

    private static PermissionGrantDto MapGrantToDto(PermissionGrant grant)
    {
        return new PermissionGrantDto
        {
            Id = grant.Id,
            TenantId = grant.TenantId,
            PermissionName = grant.PermissionName,
            ProviderName = grant.ProviderName,
            ProviderKey = grant.ProviderKey,
            GrantedAt = grant.CreatedAt
        };
    }
}
