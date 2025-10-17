using KBA.Framework.Domain.Common;
using KBA.Framework.Domain.Entities;
using KBA.ProductService.Data;
using KBA.ProductService.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KBA.ProductService.Services;

public interface IProductServiceLogic
{
    Task<List<ProductDto>> GetAllAsync();
    Task<PagedResult<ProductDto>> SearchAsync(ProductSearchParams searchParams);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto?> GetBySkuAsync(string sku);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto);
    Task<bool> UpdateStockAsync(Guid id, int quantity);
    Task DeleteAsync(Guid id);
    Task<List<string>> GetCategoriesAsync();
}

public class ProductServiceLogic : IProductServiceLogic
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductServiceLogic> _logger;

    public ProductServiceLogic(ProductDbContext context, ILogger<ProductServiceLogic> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var products = await _context.Products.ToListAsync();
        return products.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearchParams searchParams)
    {
        var query = _context.Products.AsQueryable();

        // Filtrage par terme de recherche
        if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
        {
            var searchTerm = searchParams.SearchTerm.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(searchTerm) || 
                p.Description!.ToLower().Contains(searchTerm) ||
                p.SKU.ToLower().Contains(searchTerm));
        }

        // Filtrage par catégorie
        if (!string.IsNullOrWhiteSpace(searchParams.Category))
        {
            query = query.Where(p => p.Category == searchParams.Category);
        }

        // Filtrage par prix
        if (searchParams.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= searchParams.MinPrice.Value);
        }

        if (searchParams.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= searchParams.MaxPrice.Value);
        }

        // Filtrage par disponibilité
        if (searchParams.InStock.HasValue)
        {
            if (searchParams.InStock.Value)
            {
                query = query.Where(p => p.Stock > 0);
            }
            else
            {
                query = query.Where(p => p.Stock == 0);
            }
        }

        // Comptage total avant pagination
        var totalCount = await query.CountAsync();

        // Tri
        query = searchParams.SortBy?.ToLower() switch
        {
            "price" => searchParams.SortDescending 
                ? query.OrderByDescending(p => p.Price) 
                : query.OrderBy(p => p.Price),
            "stock" => searchParams.SortDescending 
                ? query.OrderByDescending(p => p.Stock) 
                : query.OrderBy(p => p.Stock),
            "createdat" => searchParams.SortDescending 
                ? query.OrderByDescending(p => p.CreatedAt) 
                : query.OrderBy(p => p.CreatedAt),
            _ => searchParams.SortDescending 
                ? query.OrderByDescending(p => p.Name) 
                : query.OrderBy(p => p.Name)
        };

        // Pagination
        var products = await query
            .Skip(searchParams.Skip)
            .Take(searchParams.PageSize)
            .ToListAsync();

        var productDtos = products.Select(MapToDto).ToList();

        _logger.LogInformation(
            "Search completed: {Count} products found out of {Total}",
            productDtos.Count,
            totalCount
        );

        return new PagedResult<ProductDto>(
            productDtos,
            totalCount,
            searchParams.PageNumber,
            searchParams.PageSize
        );
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == sku);
        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == dto.SKU);

        if (existingProduct != null)
            throw new InvalidOperationException($"Un produit avec le SKU {dto.SKU} existe déjà");

        var product = new Product
        {
            TenantId = dto.TenantId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            SKU = dto.SKU,
            Category = dto.Category,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Produit avec l'ID {id} introuvable");

        if (!string.IsNullOrEmpty(dto.Name))
            product.UpdateDetails(dto.Name, product.Description);

        if (dto.Description != null)
            product.UpdateDetails(product.Name, dto.Description);

        if (dto.Price.HasValue)
            product.UpdatePrice(dto.Price.Value);

        if (dto.Stock.HasValue)
            product.UpdateStock(dto.Stock.Value);

        if (dto.Category != null)
            product.UpdateCategory(dto.Category);

        await _context.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Produit avec l'ID {id} introuvable");

        product.Delete();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deleted: {ProductId} - {ProductName}", product.Id, product.Name);
    }

    public async Task<bool> UpdateStockAsync(Guid id, int quantity)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Produit avec l'ID {id} introuvable");

        var oldStock = product.Stock;
        product.UpdateStock(quantity);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Stock updated for product {ProductId}: {OldStock} -> {NewStock}",
            product.Id,
            oldStock,
            quantity
        );

        return true;
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var categories = await _context.Products
            .Where(p => !string.IsNullOrEmpty(p.Category))
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return categories;
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            TenantId = product.TenantId,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Price = product.Price,
            Stock = product.Stock,
            Category = product.Category,
            CreatedAt = product.CreatedAt
        };
    }
}
