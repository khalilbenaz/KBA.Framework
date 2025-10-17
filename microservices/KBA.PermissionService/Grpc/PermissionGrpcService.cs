using Grpc.Core;
using KBA.Framework.Grpc.Permissions;
using KBA.PermissionService.DTOs;
using KBA.PermissionService.Services;

namespace KBA.PermissionService.Grpc;

/// <summary>
/// Implémentation gRPC du service de permissions
/// </summary>
public class PermissionGrpcService : Framework.Grpc.Permissions.PermissionService.PermissionServiceBase
{
    private readonly IPermissionServiceLogic _permissionService;
    private readonly ILogger<PermissionGrpcService> _logger;

    public PermissionGrpcService(
        IPermissionServiceLogic permissionService,
        ILogger<PermissionGrpcService> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    public override async Task<CheckPermissionResponse> CheckPermission(
        CheckPermissionRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC CheckPermission: UserId={UserId}, Permission={Permission}, CorrelationId={CorrelationId}",
                request.UserId,
                request.PermissionName,
                request.CorrelationId
            );

            var dto = new CheckPermissionDto
            {
                UserId = Guid.Parse(request.UserId),
                PermissionName = request.PermissionName,
                TenantId = string.IsNullOrEmpty(request.TenantId) ? null : Guid.Parse(request.TenantId)
            };

            var result = await _permissionService.CheckPermissionAsync(dto);

            return new CheckPermissionResponse
            {
                IsGranted = result.IsGranted,
                PermissionName = result.PermissionName,
                GrantedBy = result.GrantedBy ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CheckPermission");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetUserPermissionsResponse> GetUserPermissions(
        GetUserPermissionsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC GetUserPermissions: UserId={UserId}, CorrelationId={CorrelationId}",
                request.UserId,
                request.CorrelationId
            );

            var userId = Guid.Parse(request.UserId);
            var tenantId = string.IsNullOrEmpty(request.TenantId) ? (Guid?)null : Guid.Parse(request.TenantId);

            var permissions = await _permissionService.GetUserPermissionsAsync(userId, tenantId);

            var response = new GetUserPermissionsResponse();
            
            foreach (var perm in permissions)
            {
                response.Permissions.Add(new PermissionGrant
                {
                    Id = perm.Id.ToString(),
                    TenantId = perm.TenantId?.ToString() ?? string.Empty,
                    PermissionName = perm.PermissionName,
                    ProviderName = perm.ProviderName,
                    ProviderKey = perm.ProviderKey,
                    GrantedAt = perm.GrantedAt.ToString("o") // ISO 8601
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetUserPermissions");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GrantPermissionResponse> GrantPermission(
        GrantPermissionRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC GrantPermission: UserId={UserId}, Permission={Permission}",
                request.UserId,
                request.PermissionName
            );

            var dto = new GrantPermissionDto
            {
                PermissionName = request.PermissionName,
                ProviderName = request.ProviderName,
                ProviderKey = request.ProviderKey,
                TenantId = string.IsNullOrEmpty(request.TenantId) ? null : Guid.Parse(request.TenantId)
            };

            var success = await _permissionService.GrantPermissionAsync(dto);

            var response = new GrantPermissionResponse
            {
                Success = success,
                Message = success ? "Permission granted successfully" : "Permission already granted"
            };

            if (success)
            {
                // Récupérer les permissions de l'utilisateur pour obtenir la grant fraîchement créée
                var userId = Guid.Parse(request.UserId);
                var tenantId = string.IsNullOrEmpty(request.TenantId) ? (Guid?)null : Guid.Parse(request.TenantId);
                var permissions = await _permissionService.GetUserPermissionsAsync(userId, tenantId);
                var grantedPermission = permissions.FirstOrDefault(p => p.PermissionName == request.PermissionName);

                if (grantedPermission != null)
                {
                    response.Permission = new PermissionGrant
                    {
                        Id = grantedPermission.Id.ToString(),
                        TenantId = grantedPermission.TenantId?.ToString() ?? string.Empty,
                        PermissionName = grantedPermission.PermissionName,
                        ProviderName = grantedPermission.ProviderName,
                        ProviderKey = grantedPermission.ProviderKey,
                        GrantedAt = grantedPermission.GrantedAt.ToString("o")
                    };
                }
            }

            return response;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Permission not found");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GrantPermission");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<RevokePermissionResponse> RevokePermission(
        RevokePermissionRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC RevokePermission: UserId={UserId}, Permission={Permission}",
                request.UserId,
                request.PermissionName
            );

            var dto = new RevokePermissionDto
            {
                PermissionName = request.PermissionName,
                ProviderName = request.ProviderName,
                ProviderKey = request.ProviderKey,
                TenantId = string.IsNullOrEmpty(request.TenantId) ? null : Guid.Parse(request.TenantId)
            };

            var success = await _permissionService.RevokePermissionAsync(dto);

            return new RevokePermissionResponse
            {
                Success = success,
                Message = success ? "Permission revoked successfully" : "Permission not found"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC RevokePermission");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}
