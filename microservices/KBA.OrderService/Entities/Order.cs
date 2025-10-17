using KBA.Framework.Domain.Common;

namespace KBA.OrderService.Entities;

public class Order : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid? TenantId { get; private set; }
    public string Status { get; private set; } = "Pending"; // Pending, Validated, Confirmed, Shipped, Delivered, Cancelled
    public string ShippingAddress { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public new DateTime CreatedAt { get; private set; }
    public new DateTime? UpdatedAt { get; private set; }

    // Navigation property
    public List<OrderItem> Items { get; private set; } = new();

    // Constructor pour EF Core
    private Order() { }

    public Order(Guid userId, Guid? tenantId, string shippingAddress)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TenantId = tenantId;
        ShippingAddress = shippingAddress;
        Status = "Pending";
        CreatedAt = DateTime.UtcNow;
        Items = new List<OrderItem>();
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        var item = new OrderItem(Id, productId, quantity, unitPrice);
        Items.Add(item);
        RecalculateTotal();
    }

    public void UpdateStatus(string newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Validate()
    {
        UpdateStatus("Validated");
    }

    public void Confirm()
    {
        UpdateStatus("Confirmed");
    }

    public void Cancel()
    {
        UpdateStatus("Cancelled");
    }
}

public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    // Constructor pour EF Core
    private OrderItem() { }

    public OrderItem(Guid orderId, Guid productId, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public void UpdateQuantity(int newQuantity)
    {
        Quantity = newQuantity;
    }
}
