using System.Security.Claims;

namespace KBA.ProductService.Services;

/// <summary>
/// Client pour communiquer avec le Permission Service
/// </summary>
public interface IPermissionServiceClient
{
    Task<bool> CheckPermissionAsync(Guid userId, string permissionName, Guid? tenantId = null);
    Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid? tenantId = null);
}

public class PermissionServiceClient : IPermissionServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PermissionServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionServiceClient(
        IHttpClientFactory httpClientFactory,
        ILogger<PermissionServiceClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> CheckPermissionAsync(
        Guid userId, 
        string permissionName, 
        Guid? tenantId = null)
    {
        try
        {
            var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
            
            _logger.LogInformation(
                "Checking permission {PermissionName} for user {UserId} [CorrelationId: {CorrelationId}]",
                permissionName,
                userId,
                correlationId
            );

            var client = _httpClientFactory.CreateClient("PermissionService");
            
            // Propager le Correlation ID
            if (!string.IsNullOrEmpty(correlationId))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", correlationId);
            }

            var request = new CheckPermissionRequest
            {
                UserId = userId,
                PermissionName = permissionName,
                TenantId = tenantId
            };

            var response = await client.PostAsJsonAsync("/api/permissions/check", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Permission check failed with status {StatusCode} for user {UserId} and permission {PermissionName}",
                    response.StatusCode,
                    userId,
                    permissionName
                );
                return false; // Fail-safe: refuse if error
            }

            var result = await response.Content.ReadFromJsonAsync<PermissionCheckResult>();
            
            _logger.LogInformation(
                "Permission check result: {IsGranted} for user {UserId} and permission {PermissionName}",
                result?.IsGranted ?? false,
                userId,
                permissionName
            );

            return result?.IsGranted ?? false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error checking permission {PermissionName} for user {UserId}. Permission Service may be down.",
                permissionName,
                userId
            );
            return false; // Fail-safe: refuse if service unavailable
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error checking permission {PermissionName} for user {UserId}",
                permissionName,
                userId
            );
            return false; // Fail-safe: refuse if error
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid? tenantId = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PermissionService");
            
            var url = $"/api/permissions/user/{userId}";
            if (tenantId.HasValue)
            {
                url += $"?tenantId={tenantId.Value}";
            }

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get permissions for user {UserId} with status {StatusCode}",
                    userId,
                    response.StatusCode
                );
                return new List<string>();
            }

            var grants = await response.Content.ReadFromJsonAsync<List<PermissionGrantDto>>();
            
            return grants?.Select(g => g.PermissionName).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting permissions for user {UserId}",
                userId
            );
            return new List<string>();
        }
    }
}

// DTOs pour communication avec Permission Service

public class CheckPermissionRequest
{
    public Guid UserId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}

public class PermissionCheckResult
{
    public bool IsGranted { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? GrantedBy { get; set; }
}

public class PermissionGrantDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
}
