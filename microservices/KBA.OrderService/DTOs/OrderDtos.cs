namespace KBA.OrderService.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateOrderDto
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ValidateOrderDto
{
    public Guid UserId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class OrderValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool HasPermission { get; set; }
    public bool ProductsAvailable { get; set; }
    public bool UserExists { get; set; }
    public List<string> UnavailableProducts { get; set; } = new();
}
