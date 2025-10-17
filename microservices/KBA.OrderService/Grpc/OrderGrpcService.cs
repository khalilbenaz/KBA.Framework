using Grpc.Core;
using KBA.Framework.Grpc.Orders;
using KBA.OrderService.DTOs;
using KBA.OrderService.Services;

namespace KBA.OrderService.Grpc;

/// <summary>
/// Serveur gRPC pour OrderService
/// Permet à d'autres services d'appeler OrderService via gRPC
/// </summary>
public class OrderGrpcService : Framework.Grpc.Orders.OrderService.OrderServiceBase
{
    private readonly IOrderServiceLogic _orderService;
    private readonly ILogger<OrderGrpcService> _logger;

    public OrderGrpcService(
        IOrderServiceLogic orderService,
        ILogger<OrderGrpcService> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public override async Task<CreateOrderResponse> CreateOrder(
        CreateOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC CreateOrder: UserId={UserId}, ItemCount={ItemCount}, CorrelationId={CorrelationId}",
                request.UserId,
                request.Items.Count,
                request.CorrelationId
            );

            var dto = new CreateOrderDto
            {
                UserId = Guid.Parse(request.UserId),
                TenantId = string.IsNullOrEmpty(request.TenantId) ? (Guid?)null : Guid.Parse(request.TenantId),
                ShippingAddress = request.ShippingAddress,
                Items = request.Items.Select(i => new CreateOrderItemDto
                {
                    ProductId = Guid.Parse(i.ProductId),
                    Quantity = i.Quantity,
                    UnitPrice = (decimal)i.UnitPrice
                }).ToList()
            };

            var order = await _orderService.CreateOrderAsync(dto);

            return new CreateOrderResponse
            {
                Success = true,
                Message = "Order created successfully",
                Order = MapToGrpcOrder(order)
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CreateOrder");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetOrderResponse> GetOrder(
        GetOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC GetOrder: OrderId={OrderId}, CorrelationId={CorrelationId}",
                request.OrderId,
                request.CorrelationId
            );

            var orderId = Guid.Parse(request.OrderId);
            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                return new GetOrderResponse { Success = false };
            }

            return new GetOrderResponse
            {
                Success = true,
                Order = MapToGrpcOrder(order)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetOrder");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetUserOrdersResponse> GetUserOrders(
        GetUserOrdersRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC GetUserOrders: UserId={UserId}, CorrelationId={CorrelationId}",
                request.UserId,
                request.CorrelationId
            );

            var userId = Guid.Parse(request.UserId);
            var tenantId = string.IsNullOrEmpty(request.TenantId) ? (Guid?)null : Guid.Parse(request.TenantId);

            var orders = await _orderService.GetUserOrdersAsync(userId, tenantId);

            var response = new GetUserOrdersResponse
            {
                TotalCount = orders.Count
            };

            foreach (var order in orders)
            {
                response.Orders.Add(MapToGrpcOrder(order));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetUserOrders");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<ValidateOrderResponse> ValidateOrder(
        ValidateOrderRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "gRPC ValidateOrder: UserId={UserId}, CorrelationId={CorrelationId}",
                request.UserId,
                request.CorrelationId
            );

            var dto = new ValidateOrderDto
            {
                UserId = Guid.Parse(request.UserId),
                Items = request.Items.Select(i => new CreateOrderItemDto
                {
                    ProductId = Guid.Parse(i.ProductId),
                    Quantity = i.Quantity,
                    UnitPrice = (decimal)i.UnitPrice
                }).ToList()
            };

            var result = await _orderService.ValidateOrderAsync(dto);

            var response = new ValidateOrderResponse
            {
                IsValid = result.IsValid,
                Details = new ValidationDetails
                {
                    HasPermission = result.HasPermission,
                    ProductsAvailable = result.ProductsAvailable,
                    UserExists = result.UserExists
                }
            };

            response.Errors.AddRange(result.Errors);
            response.Details.UnavailableProducts.AddRange(result.UnavailableProducts);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC ValidateOrder");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    private static Order MapToGrpcOrder(OrderDto order)
    {
        var grpcOrder = new Framework.Grpc.Orders.Order
        {
            Id = order.Id.ToString(),
            UserId = order.UserId.ToString(),
            TenantId = order.TenantId.HasValue ? order.TenantId.Value.ToString() : string.Empty,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            TotalAmount = (double)order.TotalAmount,
            CreatedAt = order.CreatedAt.ToString("o"),
            UpdatedAt = order.UpdatedAt?.ToString("o") ?? string.Empty
        };

        foreach (var item in order.Items)
        {
            grpcOrder.Items.Add(new OrderItem
            {
                ProductId = item.ProductId.ToString(),
                Quantity = item.Quantity,
                UnitPrice = (double)item.UnitPrice
            });
        }

        return grpcOrder;
    }
}
