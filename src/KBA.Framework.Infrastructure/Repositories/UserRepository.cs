using KBA.Framework.Domain.Entities.Identity;
using KBA.Framework.Domain.Repositories;
using KBA.Framework.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KBA.Framework.Infrastructure.Repositories;

/// <summary>
/// Repository pour les utilisateurs
/// </summary>
public class UserRepository : Repository<User, Guid>, IUserRepository
{
    /// <summary>
    /// Constructeur
    /// </summary>
    public UserRepository(KBADbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = userName.ToUpperInvariant();
        // Ne pas utiliser AsNoTracking ici car cette méthode peut être utilisée pour l'authentification
        // où le tracking pourrait être nécessaire pour mettre à jour l'utilisateur
        return await _dbSet
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }
}
