using KBA.Framework.Domain.Common;

namespace KBA.Framework.Domain.Entities;

/// <summary>
/// Entité produit pour les microservices
/// </summary>
public class Product : AggregateRoot
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;

    public void UpdateStock(int quantity)
    {
        Stock = quantity;
        MarkAsUpdated();
    }

    public void DecreaseStock(int quantity)
    {
        if (Stock < quantity)
            throw new InvalidOperationException("Stock insuffisant");
        
        Stock -= quantity;
        MarkAsUpdated();
    }

    public void IncreaseStock(int quantity)
    {
        Stock += quantity;
        MarkAsUpdated();
    }

    public void UpdateDetails(string name, string? description)
    {
        Name = name;
        Description = description;
        MarkAsUpdated();
    }

    public void UpdatePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Le prix ne peut pas être négatif", nameof(price));
        
        Price = price;
        MarkAsUpdated();
    }

    public void UpdateCategory(string? category)
    {
        Category = category;
        MarkAsUpdated();
    }

    public void Delete()
    {
        MarkAsDeleted();
    }
}
