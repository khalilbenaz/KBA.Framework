using KBA.Framework.Domain.Common;

namespace KBA.ProductService.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string SKU { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public bool IsAvailable => Stock > 0;
    public DateTime CreatedAt { get; set; }
}

public class CreateProductDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string SKU { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public Guid? TenantId { get; set; }
}

public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Paramètres de recherche et filtrage des produits
/// </summary>
public class ProductSearchParams : PaginationParams
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public string? SortBy { get; set; } = "name"; // name, price, stock, createdAt
    public bool SortDescending { get; set; } = false;
}
