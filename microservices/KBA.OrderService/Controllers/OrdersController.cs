using KBA.Framework.Domain.Common;
using KBA.OrderService.DTOs;
using KBA.OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KBA.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderServiceLogic _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderServiceLogic orderService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Crée une nouvelle commande
    /// EXEMPLE : Utilise gRPC pour communiquer avec IdentityService et PermissionService
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? dto.UserId.ToString());
            dto.UserId = userId;

            var order = await _orderService.CreateOrderAsync(dto);

            return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order created successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Récupère une commande par ID
    /// </summary>
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);

        if (order == null)
            return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));

        return Ok(ApiResponse<OrderDto>.SuccessResponse(order));
    }

    /// <summary>
    /// Récupère toutes les commandes d'un utilisateur
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserOrders(Guid userId)
    {
        var orders = await _orderService.GetUserOrdersAsync(userId);

        return Ok(ApiResponse<List<OrderDto>>.SuccessResponse(orders));
    }

    /// <summary>
    /// Valide une commande (vérifie tous les services)
    /// EXEMPLE : Orchestre plusieurs appels gRPC vers différents services
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateOrder([FromBody] ValidateOrderDto dto)
    {
        try
        {
            var result = await _orderService.ValidateOrderAsync(dto);

            if (!result.IsValid)
            {
                return BadRequest(ApiResponse<OrderValidationResult>.ErrorResponse("Order validation failed"));
            }

            return Ok(ApiResponse<OrderValidationResult>.SuccessResponse(result, "Order is valid"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order");
            return StatusCode(500, ApiResponse<OrderValidationResult>.ErrorResponse("Internal server error"));
        }
    }
}
