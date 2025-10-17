using Grpc.Core;
using KBA.Framework.Grpc.Identity;
using KBA.IdentityService.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace KBA.IdentityService.Grpc;

/// <summary>
/// Implémentation gRPC du service d'identité
/// </summary>
public class IdentityGrpcService : Framework.Grpc.Identity.IdentityService.IdentityServiceBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityGrpcService> _logger;

    public IdentityGrpcService(
        IUserService userService,
        IConfiguration configuration,
        ILogger<IdentityGrpcService> logger)
    {
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task<ValidateTokenResponse> ValidateToken(
        ValidateTokenRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC ValidateToken: CorrelationId={CorrelationId}",
                request.CorrelationId
            );

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey manquante");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out var validatedToken);

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
            var tenantId = principal.FindFirst("TenantId")?.Value ?? string.Empty;

            // Récupérer les rôles (si disponibles)
            var roles = principal.FindAll("role").Select(c => c.Value).ToList();

            return new ValidateTokenResponse
            {
                IsValid = true,
                UserId = userId,
                Email = email,
                TenantId = tenantId,
                Roles = { roles }
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid token");
            return new ValidateTokenResponse
            {
                IsValid = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC ValidateToken");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetUserResponse> GetUser(
        GetUserRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC GetUser: UserId={UserId}, CorrelationId={CorrelationId}",
                request.UserId,
                request.CorrelationId
            );

            var userId = Guid.Parse(request.UserId);
            var userDto = await _userService.GetByIdAsync(userId);

            if (userDto == null)
            {
                return new GetUserResponse
                {
                    Success = false
                };
            }

            return new GetUserResponse
            {
                Success = true,
                User = new User
                {
                    Id = userDto.Id.ToString(),
                    Email = userDto.Email,
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    TenantId = userDto.TenantId?.ToString() ?? string.Empty,
                    IsActive = userDto.IsActive,
                    CreatedAt = DateTime.UtcNow.ToString("o") // Placeholder, should be from actual user data
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetUser");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<UserExistsResponse> UserExists(
        UserExistsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC UserExists: UserId={UserId}, CorrelationId={CorrelationId}",
                request.UserId,
                request.CorrelationId
            );

            var userId = Guid.Parse(request.UserId);
            var userDto = await _userService.GetByIdAsync(userId);

            return new UserExistsResponse
            {
                Exists = userDto != null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC UserExists");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetUserRolesResponse> GetUserRoles(
        GetUserRolesRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC GetUserRoles: UserId={UserId}, CorrelationId={CorrelationId}",
                request.UserId,
                request.CorrelationId
            );

            // Pour l'instant, retourner une liste vide
            // Dans une vraie implémentation, il faudrait avoir un service de rôles
            var response = new GetUserRolesResponse();
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetUserRoles");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}
