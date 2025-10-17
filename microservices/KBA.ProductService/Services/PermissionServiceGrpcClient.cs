using Grpc.Core;
using Grpc.Net.Client;
using KBA.Framework.Grpc.Permissions;

namespace KBA.ProductService.Services;

/// <summary>
/// Client gRPC pour communiquer avec le Permission Service
/// </summary>
public interface IPermissionServiceGrpcClient
{
    Task<bool> CheckPermissionAsync(Guid userId, string permissionName, Guid? tenantId = null);
    Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid? tenantId = null);
}

public class PermissionServiceGrpcClient : IPermissionServiceGrpcClient
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PermissionServiceGrpcClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionServiceGrpcClient(
        IConfiguration configuration,
        ILogger<PermissionServiceGrpcClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
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
            var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? string.Empty;

            _logger.LogInformation(
                "gRPC CheckPermission: UserId={UserId}, Permission={PermissionName}, CorrelationId={CorrelationId}",
                userId,
                permissionName,
                correlationId
            );

            var grpcUrl = _configuration["GrpcServices:PermissionService"] ?? "http://localhost:5004";
            
            using var channel = GrpcChannel.ForAddress(grpcUrl);
            var client = new Framework.Grpc.Permissions.PermissionService.PermissionServiceClient(channel);

            var request = new Framework.Grpc.Permissions.CheckPermissionRequest
            {
                UserId = userId.ToString(),
                PermissionName = permissionName,
                TenantId = tenantId.HasValue ? tenantId.Value.ToString() : string.Empty,
                CorrelationId = correlationId
            };

            var response = await client.CheckPermissionAsync(request);

            _logger.LogInformation(
                "Permission check result: {IsGranted} for user {UserId} and permission {PermissionName}",
                response.IsGranted,
                userId,
                permissionName
            );

            return response.IsGranted;
        }
        catch (RpcException ex)
        {
            _logger.LogError(
                ex,
                "gRPC error checking permission {PermissionName} for user {UserId}. Status: {Status}",
                permissionName,
                userId,
                ex.Status
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
            var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? string.Empty;

            _logger.LogInformation(
                "gRPC GetUserPermissions: UserId={UserId}, CorrelationId={CorrelationId}",
                userId,
                correlationId
            );

            var grpcUrl = _configuration["GrpcServices:PermissionService"] ?? "http://localhost:5004";
            
            using var channel = GrpcChannel.ForAddress(grpcUrl);
            var client = new Framework.Grpc.Permissions.PermissionService.PermissionServiceClient(channel);

            var request = new Framework.Grpc.Permissions.GetUserPermissionsRequest
            {
                UserId = userId.ToString(),
                TenantId = tenantId.HasValue ? tenantId.Value.ToString() : string.Empty,
                CorrelationId = correlationId
            };

            var response = await client.GetUserPermissionsAsync(request);

            return response.Permissions.Select(p => p.PermissionName).ToList();
        }
        catch (RpcException ex)
        {
            _logger.LogError(
                ex,
                "gRPC error getting permissions for user {UserId}. Status: {Status}",
                userId,
                ex.Status
            );
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error getting permissions for user {UserId}",
                userId
            );
            return new List<string>();
        }
    }
}
