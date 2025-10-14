using KBA.Framework.Domain.Entities.Products;
using KBA.Framework.Domain.Repositories;
using KBA.Framework.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KBA.Framework.Infrastructure.Repositories;

/// <summary>
/// Repository pour les produits
/// </summary>
public class ProductRepository : Repository<Product, Guid>, IProductRepository
{
    /// <summary>
    /// Constructeur
    /// </summary>
    public ProductRepository(KBADbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Product>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Name.Contains(name))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
