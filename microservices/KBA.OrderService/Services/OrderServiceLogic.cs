using Grpc.Core;
using Grpc.Net.Client;
using KBA.Framework.Grpc.Identity;
using KBA.Framework.Grpc.Permissions;
using KBA.OrderService.Data;
using KBA.OrderService.DTOs;
using KBA.OrderService.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBA.OrderService.Services;

/// <summary>
/// Service de gestion des commandes
/// EXEMPLE : Ce service dépend de PLUSIEURS autres services via gRPC
/// </summary>
public interface IOrderServiceLogic
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId);
    Task<List<OrderDto>> GetUserOrdersAsync(Guid userId, Guid? tenantId = null);
    Task<OrderValidationResult> ValidateOrderAsync(ValidateOrderDto dto);
}

public class OrderServiceLogic : IOrderServiceLogic
{
    private readonly OrderDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderServiceLogic> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrderServiceLogic(
        OrderDbContext context,
        IConfiguration configuration,
        ILogger<OrderServiceLogic> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Crée une commande en validant via PLUSIEURS services gRPC :
    /// - IdentityService : Vérifier que l'utilisateur existe
    /// - PermissionService : Vérifier les permissions
    /// - ProductService (si implémenté) : Vérifier le stock
    /// </summary>
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Creating order for user {UserId} with {ItemCount} items [CorrelationId: {CorrelationId}]",
            dto.UserId,
            dto.Items.Count,
            correlationId
        );

        // 1. Vérifier que l'utilisateur existe via IdentityService (gRPC)
        var userExists = await CheckUserExistsAsync(dto.UserId, correlationId);
        if (!userExists)
        {
            throw new InvalidOperationException($"User {dto.UserId} not found");
        }

        // 2. Vérifier les permissions via PermissionService (gRPC)
        var hasPermission = await CheckPermissionAsync(dto.UserId, "Orders.Create", dto.TenantId, correlationId);
        if (!hasPermission)
        {
            throw new UnauthorizedAccessException($"User {dto.UserId} does not have permission to create orders");
        }

        // 3. TODO: Vérifier le stock des produits via ProductService (gRPC)
        // Pour cet exemple, on suppose que les produits sont disponibles

        // 4. Créer la commande
        var order = new Order(dto.UserId, dto.TenantId, dto.ShippingAddress);

        foreach (var item in dto.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Order {OrderId} created successfully for user {UserId} - Total: {TotalAmount} [CorrelationId: {CorrelationId}]",
            order.Id,
            dto.UserId,
            order.TotalAmount,
            correlationId
        );

        return MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order == null ? null : MapToDto(order);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(Guid userId, Guid? tenantId = null)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId);

        if (tenantId.HasValue)
        {
            query = query.Where(o => o.TenantId == tenantId.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Valide une commande en vérifiant TOUS les services nécessaires
    /// EXEMPLE COMPLET d'orchestration de plusieurs services gRPC
    /// </summary>
    public async Task<OrderValidationResult> ValidateOrderAsync(ValidateOrderDto dto)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        var errors = new List<string>();
        var result = new OrderValidationResult
        {
            IsValid = true,
            Errors = errors
        };

        _logger.LogInformation(
            "Validating order for user {UserId} [CorrelationId: {CorrelationId}]",
            dto.UserId,
            correlationId
        );

        try
        {
            // 1. Vérifier l'utilisateur via IdentityService (gRPC)
            _logger.LogInformation("Step 1/3: Checking user existence via IdentityService gRPC...");
            var userExists = await CheckUserExistsAsync(dto.UserId, correlationId);
            result.UserExists = userExists;

            if (!userExists)
            {
                errors.Add($"User {dto.UserId} does not exist");
                result.IsValid = false;
            }
            else
            {
                _logger.LogInformation("✅ User exists");
            }

            // 2. Vérifier les permissions via PermissionService (gRPC)
            _logger.LogInformation("Step 2/3: Checking permissions via PermissionService gRPC...");
            var hasPermission = await CheckPermissionAsync(dto.UserId, "Orders.Create", null, correlationId);
            result.HasPermission = hasPermission;

            if (!hasPermission)
            {
                errors.Add($"User {dto.UserId} does not have 'Orders.Create' permission");
                result.IsValid = false;
            }
            else
            {
                _logger.LogInformation("✅ User has required permissions");
            }

            // 3. TODO: Vérifier la disponibilité des produits via ProductService (gRPC)
            _logger.LogInformation("Step 3/3: Checking product availability (simulated)...");
            result.ProductsAvailable = true; // Simulé pour l'exemple
            result.UnavailableProducts = new List<string>();

            _logger.LogInformation(
                "Order validation completed: {IsValid} [CorrelationId: {CorrelationId}]",
                result.IsValid,
                correlationId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during order validation");
            errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    // ========== CLIENTS gRPC VERS AUTRES SERVICES ==========

    /// <summary>
    /// Appelle IdentityService via gRPC pour vérifier si un utilisateur existe
    /// </summary>
    private async Task<bool> CheckUserExistsAsync(Guid userId, string correlationId)
    {
        try
        {
            var grpcUrl = _configuration["GrpcServices:IdentityService"] ?? "http://localhost:5001";

            _logger.LogDebug("Connecting to IdentityService gRPC at {Url}", grpcUrl);

            using var channel = GrpcChannel.ForAddress(grpcUrl);
            var client = new Framework.Grpc.Identity.IdentityService.IdentityServiceClient(channel);

            var request = new UserExistsRequest
            {
                UserId = userId.ToString(),
                CorrelationId = correlationId
            };

            var response = await client.UserExistsAsync(request);

            _logger.LogInformation(
                "IdentityService gRPC response: User {UserId} exists = {Exists}",
                userId,
                response.Exists
            );

            return response.Exists;
        }
        catch (RpcException ex)
        {
            _logger.LogError(
                ex,
                "gRPC error calling IdentityService for user {UserId}. Status: {Status}",
                userId,
                ex.Status
            );
            return false;
        }
    }

    /// <summary>
    /// Appelle PermissionService via gRPC pour vérifier une permission
    /// </summary>
    private async Task<bool> CheckPermissionAsync(
        Guid userId,
        string permissionName,
        Guid? tenantId,
        string correlationId)
    {
        try
        {
            var grpcUrl = _configuration["GrpcServices:PermissionService"] ?? "http://localhost:5004";

            _logger.LogDebug("Connecting to PermissionService gRPC at {Url}", grpcUrl);

            using var channel = GrpcChannel.ForAddress(grpcUrl);
            var client = new Framework.Grpc.Permissions.PermissionService.PermissionServiceClient(channel);

            var request = new CheckPermissionRequest
            {
                UserId = userId.ToString(),
                PermissionName = permissionName,
                TenantId = tenantId?.ToString() ?? string.Empty,
                CorrelationId = correlationId
            };

            var response = await client.CheckPermissionAsync(request);

            _logger.LogInformation(
                "PermissionService gRPC response: User {UserId} has permission '{Permission}' = {IsGranted}",
                userId,
                permissionName,
                response.IsGranted
            );

            return response.IsGranted;
        }
        catch (RpcException ex)
        {
            _logger.LogError(
                ex,
                "gRPC error calling PermissionService for user {UserId} and permission {Permission}. Status: {Status}",
                userId,
                permissionName,
                ex.Status
            );
            return false;
        }
    }

    // ========== HELPERS ==========

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TenantId = order.TenantId,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }
}
